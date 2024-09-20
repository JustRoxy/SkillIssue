FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SkillIssue.Players/SkillIssue.Players.csproj", "SkillIssue.Players/"]
RUN dotnet restore "SkillIssue.Players/SkillIssue.Players.csproj"
COPY . .
WORKDIR "/src/SkillIssue.Players"
RUN dotnet build "SkillIssue.Players.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SkillIssue.Players.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SkillIssue.Players.dll"]
