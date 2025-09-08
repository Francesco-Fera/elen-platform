FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/WorkflowEngine.API/WorkflowEngine.API.csproj", "src/WorkflowEngine.API/"]
COPY ["src/WorkflowEngine.Application/WorkflowEngine.Application.csproj", "src/WorkflowEngine.Application/"]
COPY ["src/WorkflowEngine.Core/WorkflowEngine.Core.csproj", "src/WorkflowEngine.Core/"]
COPY ["src/WorkflowEngine.Execution/WorkflowEngine.Execution.csproj", "src/WorkflowEngine.Execution/"]
COPY ["src/WorkflowEngine.Infrastructure/WorkflowEngine.Infrastructure.csproj", "src/WorkflowEngine.Infrastructure/"]
COPY ["src/WorkflowEngine.Nodes/WorkflowEngine.Nodes.csproj", "src/WorkflowEngine.Nodes/"]
RUN dotnet restore "./src/WorkflowEngine.API/WorkflowEngine.API.csproj"
COPY . .
WORKDIR "/src/src/WorkflowEngine.API"
RUN dotnet build "./WorkflowEngine.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WorkflowEngine.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkflowEngine.API.dll"]
