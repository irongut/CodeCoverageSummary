FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/CodeCoverageSummary/CodeCoverageSummary.csproj", "CodeCoverageSummary/"]
RUN dotnet restore CodeCoverageSummary/CodeCoverageSummary.csproj
COPY ["src/", "CodeCoverageSummary/"]
RUN dotnet build CodeCoverageSummary/CodeCoverageSummary.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish CodeCoverageSummary/CodeCoverageSummary.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeCoverageSummary.dll"]