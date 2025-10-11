# tests/UsersService.Tests/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS testrunner
WORKDIR /src

# copy solution & project files first (restore cache)
COPY CICD_Example.sln .
COPY services/UsersService/UsersService.csproj services/UsersService/
COPY tests/UsersService.Tests/UsersService.Tests.csproj tests/UsersService.Tests/

RUN dotnet restore

# copy the rest and run tests
COPY . .
CMD ["dotnet", "test", "CICD_Example.sln", "--configuration", "Release", "--logger:trx;LogFileName=test_results.trx"]
