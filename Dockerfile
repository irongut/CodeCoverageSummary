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

# Label the container
LABEL maintainer="Irongut <murray.dave@outlook.com>"
LABEL repository="https://github.com/irongut/CodeCoverageSummary"
LABEL homepage="https://github.com/irongut/CodeCoverageSummary"

# Label as GitHub Action
LABEL com.github.actions.name="Code Coverage Summary"
LABEL com.github.actions.description="A GitHub Action that reads Cobertura format code coverage files and outputs a summary."
LABEL com.github.actions.icon="book-open"
LABEL com.github.actions.color="purple"

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeCoverageSummary.dll"]