**Users Service – CI/CD Example (.NET 8 + Docker + AWS)**

This repository demonstrates a production-style workflow for a simple ASP.NET Core microservice with automated tests and a complete CI/CD pipeline on AWS (GitHub → CodeBuild → CodeDeploy → EC2). It’s intentionally small but realistic, with JWT authentication, MongoDB persistence, and Dockerized deployment.
--------------------------------------------------------
1) The Microservice:
  * Tech stack:
    * ASP.NET Core 8 (minimal hosting model)
    * MongoDB driver for persistence
    * JWT for authentication (HMAC SHA-256 signing)
    * BCrypt.Net-Next for password hashing
    * Swashbuckle (OpenAPI/Swagger) for API docs
    * Docker / Docker Compose for containerization

  * Domain: Users
    * POST /users/register: Creates a user, returns 201 Created with the created user. On invalid input → 400 Bad Request. On duplicate email → 409 Conflict (via custom DuplicateEmailException → handled by controller).
    * GET /users/login (the sample uses GET in a few places during dev; prefer POST). Validates credentials, returns a JWT token.
    * PUT /users/{user_id}: Updates user's data.

  * Controller highlights
    * UsersController is [ApiController] with [Route("users")].
    * Key error handling we validated in tests:
    * DuplicateEmailException → ConflictObjectResult (409)
    * ArgumentNullException → BadRequest (400)
    * Unexpected exceptions → StatusCode(500) (initially we mapped to 409, later corrected).

  * Configuration (env vars / appsettings)
    * JWT:SecretKey, JWT:Issuer, JWT:Audience, JWT:ExpiryMinutes (must be > 0, use UTC!)
    * Mongo:ConnectionString, Mongo:Database, Mongo:UsersCollectionName
-----------------------------------------------------------
2) Tests section:
  * 201 Created when registration succeeds (verifies CreatedAtAction, action name, and returned user).
  * 409 Conflict when email already exists (throws DuplicateEmailException from service).
  * 400 BadRequest for invalid args (e.g., null input → ArgumentNullException).
  * 500 on unexpected exceptions (surface as server errors).
------------------------------------------------------------------
3) CI/CD on AWS:
   
    High-level flow: GitHub (source) → AWS CodeBuild (build + tests) → Amazon ECR (image) → AWS CodeDeploy → EC2 (Docker Compose up)

    Services in detail:
    * GitHub: CodePipeline’s Source stage pulls this branch on commits/PR merges. Tip: Protect main branches; run PR checks with CodeBuild.
    * Amazon ECR (Elastic Container Registry): Stores the built Docker image: cicd-example-usersservice:latest. You log in from EC2 during deploy (scripted) using aws ecr get-login-password. Watch for: account/region in the ECR URL and required permission to pull.
    * AWS CodeBuild – build & test. Uses buildspec.yml. Steps:
      * Restore dependencies (dotnet restore)
      * Build solution (dotnet build -c Release)
      * Run tests (dotnet test -c Release)
      * (Optional) Build/push image if you do that in this stage; in our flow we push from CodeBuild or let CodeDeploy pull ECR.
    * IAM (CodeBuild service role) needs permissions to pull dependencies, push to ECR (if pushing here), and read from GitHub.
    * Watch for:
      * Right .NET SDK on the build container.
      * Ensure test failures fail the build.
      * Avoid duplicate PackageReference versions in .csproj.
    * AWS CodeDeploy – orchestration to EC2:
      * Uses appspec.yml (in deploy/) to define lifecycle hooks (e.g., ApplicationStart).
      * It copies the deployment bundle to the EC2 host, then calls our scripts (deploy/start.sh).
      * CodeDeploy agent: must be installed and running on EC2.
      * Watch for:
        * Script exit codes. A non-zero exit fails the deployment.
        * The deployment user (we run as ubuntu), file permissions & shebang (#!/usr/bin/env bash).
        * Environment needed by the script (e.g., AWS_ACCOUNT_ID, region).
    * Amazon EC2 – the runtime:
        * Ubuntu host with Docker and Docker Compose installed.
        * Instance has an IAM role that allows:
          * Pull from ECR (if you log in using aws ecr get-login-password on the host)
        * Watch for:
          * Time sync (NTP) — prevents JWT time skew errors.
          * Disk space and Docker cleanup (--remove-orphans).
          * Public IP/DNS and security group rules for Postman to reach http://<EC2_PUBLIC_IP>.
    * AWS CodePipeline – the pipeline
      * Stages: Source (GitHub) → Build (CodeBuild) → Deploy (CodeDeploy).
      * Artifacts flow between stages automatically.
      * Watch for:
        * Proper artifact names and locations from CodeBuild.
        * Stage permissions (the CodePipeline role must assume CodeBuild/CodeDeploy roles).
    * AWS IAM – roles and permissions:
      * Minimum roles/policies we used:
      * CodeBuild service role — access to S3 artifacts, ECR (if pushing), CloudWatch logs.
      * CodeDeploy service role — permission to interact with deployment groups/EC2 instances.
      * EC2 instance role — permission to pull ECR images.
      * Pipeline role — can call CodeBuild/CodeDeploy and read from GitHub (via connection).
----------------------------------------------
4) YAML files in the project:
   * buildspec.yml: Used by CodeBuild
   * docker-compose.prod.yml / docker-compose.rendered.yml:
     * Compose definition for the users-service container.
     * rendered is the version after templating variables (ECR image URL, env vars).
     * Ports: container’s 80 exposed on host 8080 (or published 80→8080 depending on your final mapping).
   * appspec.yml: CodeDeploy manifest.
----------------------------------------------
5) Files in the deploy/ folder:
   * start.sh – deployment entrypoint run by CodeDeploy:
     * cd /home/ubuntu/app
     * Export region defaults
     * Login to ECR with aws ecr get-login-password
     * docker compose -f docker-compose.rendered.yml pull
     * docker compose -f docker-compose.rendered.yml up -d --remove-orphans
     * docker ps to display status
   * (Optional) stop.sh / health.sh if you later add extra hooks (e.g., BeforeInstall, ValidateService).
----------------------------------------------------
6) How to run on EC2 and test from your PC:
   A) One-time EC2 setup
     * Provision EC2 (Ubuntu 22.04 or similar) in the right VPC/region (eu-west-1 in our case).
     * Install Docker (See below)
     * Install CodeDeploy agent
     * Security group
       * Inbound:
         * 22 from your IP (SSH)
         * 80 from 0.0.0.0/0 (for quick testing. Can be restricted to your IP range in real setups)
       * Outbound: allow all (or at least ECR/updates).
     * IAM instance profile:
       * Attach a role that allows ecr:GetAuthorizationToken, ecr:BatchGetImage, ecr:GetDownloadUrlForLayer
   B) Deploy via CodePipeline/CodeDeploy:
    * Commit/push to GitHub → CodePipeline triggers → CodeBuild compiles & tests → CodeDeploy runs deploy/start.sh on EC2.
    * Watch the deploy in CodeDeploy console; if a script fails, check the logs the console points to.
   C) Validate on the instance (See below)
   D) Call the API from your PC (Postman / curl)
  
------------------------------------------
Install Docker on EC2 instance:
<blockquote>
<br>sudo apt-get update -y
<br>sudo apt-get install -y ca-certificates curl gnupg
<br>sudo install -m 0755 -d /etc/apt/keyrings
<br>curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
<br>echo \
<br>  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
<br>  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
<br>  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
<br>sudo apt-get update -y
<br>sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
<br>sudo usermod -aG docker ubuntu
</blockquote>
----------------------------------------------------
Validate on the EC2 instance:

<blockquote>
<br>ssh -i <your-key.pem> ubuntu@<EC2_PUBLIC_IP>
<br>cd /home/ubuntu/app
<br>docker compose -f docker-compose.rendered.yml ps
<br>docker logs <users-service-container-id> --tail=300
</blockquote>
----------------------------------------------------
Register request:
<blockquote>
<br>POST http://<EC2_PUBLIC_IP>/users/register
<br>Content-Type: application/json

<br>{
<br>  "firstName": "...",
<br>  "lastName": "...",
<br>  "email": "...@...",
<br>  "password": "...", (Saved as encrypted string)
<br>  "role": "Guest", "User", or "Admin",
<br>  "birthDate": "yyyy-mm-ddThh:mm:ssZ"
<br>}
</blockquote>
---------------------------------------------------
Login Request:
<blockquote>
<br>POST http://<EC2_PUBLIC_IP>/users/login
<br>Content-Type: application/json

<br>{
<br>  "email": "...@...",
  "password": "..." (original password)
<br>}
</blockquote>
--------------------------------------------------
Edit Request:
<blockquote>
<br>PUT http://<EC2_PUBLIC_IP>/users/{user_id}
<br>Content-Type: application/json
<br>Authorization: Bearer Token: Token (given in a successful login response)

<br>(Not all fields are required, but at least one is required.)
<br>{
<br>  "firstName": "...",
<br>  "lastName": "...",
<br>  "email": "...@...",
<br>  "password": "...",
<br>  "birthDate": "yyyy-mm-ddThh:mm:ssZ"
<br>  "role": "Guest", "User", or "Admin"
<br>}
<blockquote>
---------------------------------------------------
Appendix – Quick reference (env vars). Set these in your compose or as EC2 environment variables:

<br># JWT
<br>JWT__SecretKey=<long-random-32+ chars>
<br>JWT__Issuer=CICD_Proj.Auth
<br>JWT__Audience=CICD_Proj.Users
<br>JWT__ExpiryMinutes=60

<br># Mongo
<br>Mongo__ConnectionString=mongodb://<user>:<pass>@<host>:27017
<br>Mongo__Database=usersdb
<br>Mongo__UsersCollectionName=users

<br># AWS/ECR (used by start.sh)
<br>AWS_ACCOUNT_ID=<your-account-id>
<br>AWS_REGION=eu-west-1
