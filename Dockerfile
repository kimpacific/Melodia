# Optional: build Melodia and run its unit tests in a Linux container.
# This does NOT build the Windows installer (Inno Setup and the .exe need Windows).
# Build:  docker build -t melodia-tests .
# Run:    docker run --rm melodia-tests
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a .csproj changes
COPY Melodia/Melodia.csproj Melodia/
COPY Melodia.Tests/Melodia.Tests.csproj Melodia.Tests/
RUN dotnet restore Melodia.Tests/Melodia.Tests.csproj

COPY Melodia/ Melodia/
COPY Melodia.Tests/ Melodia.Tests/
RUN dotnet build Melodia.Tests/Melodia.Tests.csproj -c Release --no-restore

CMD ["dotnet", "test", "Melodia.Tests/Melodia.Tests.csproj", "-c", "Release", "--no-build"]