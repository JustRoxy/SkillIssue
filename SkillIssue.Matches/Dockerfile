FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SkillIssue.Matches/SkillIssue.Matches.csproj", "SkillIssue.Matches/"]
RUN dotnet restore "SkillIssue.Matches/SkillIssue.Matches.csproj"
COPY . .
WORKDIR "/src/SkillIssue.Matches"
RUN dotnet build "SkillIssue.Matches.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SkillIssue.Matches.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SkillIssue.Matches.dll"]
