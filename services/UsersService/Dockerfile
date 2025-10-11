FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY UsersService.csproj ./
RUN dotnet restore UsersService.csproj
COPY . .
RUN dotnet publish ./UsersService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
CMD ["dotnet", "UsersService.dll"]