FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SkillIssue.Mappooling/SkillIssue.Mappooling.csproj", "SkillIssue.Mappooling/"]
RUN dotnet restore "SkillIssue.Mappooling/SkillIssue.Mappooling.csproj"
COPY . .
WORKDIR "/src/SkillIssue.Mappooling"
RUN dotnet build "SkillIssue.Mappooling.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SkillIssue.Mappooling.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SkillIssue.Mappooling.dll"]
