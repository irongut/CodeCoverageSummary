FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/CodeCoverageSummary/CodeCoverageSummary.csproj", "CodeCoverageSummary/"]
RUN dotnet restore CodeCoverageSummary/CodeCoverageSummary.csproj
COPY ["src/CodeCoverageSummary", "CodeCoverageSummary/"]
COPY ["src/coverage.cobertura.xml", "sample.coverage.xml"]
RUN dotnet build CodeCoverageSummary/CodeCoverageSummary.csproj --configuration Release --no-restore --output /app/build
RUN dotnet publish CodeCoverageSummary/CodeCoverageSummary.csproj --configuration Release --no-restore --output /app/publish

# Label the container
LABEL maintainer="Irongut <murray.dave@outlook.com>"
LABEL repository="https://github.com/irongut/CodeCoverageSummary"
LABEL homepage="https://github.com/irongut/CodeCoverageSummary"

# Label as GitHub Action
LABEL com.github.actions.name="Code Coverage Summary"
LABEL com.github.actions.description="A GitHub Action that reads Cobertura format code coverage files and outputs a summary."
LABEL com.github.actions.icon="book-open"
LABEL com.github.actions.color="purple"

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/sample.coverage.xml .
ENTRYPOINT ["dotnet", "/app/CodeCoverageSummary.dll"]