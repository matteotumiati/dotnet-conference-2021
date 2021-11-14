# Set the base image (.NET 6.0 SDK+runtime)
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . ./
RUN dotnet publish ./DotNetCollectorForGitHub/DotNetCollectorForGitHub.csproj -c Release -o out --no-self-contained

# Label the container
LABEL maintainer="Matteo Tumiati <matteot@aspitalia.com>"
LABEL repository="https://github.com/matteotumiati/dotnet-conference-2021"
LABEL homepage="https://github.com/matteotumiati/dotnet-conference-2021"

# Label as GitHub action
LABEL com.github.actions.name="DotNetCollectorForGitHub"
LABEL com.github.actions.description="Collect information regarding pipeline usage"
LABEL com.github.actions.icon="activity"
LABEL com.github.actions.color="blue"

# Use .NET runtime to publish the container 
FROM mcr.microsoft.com/dotnet/sdk:6.0
COPY --from=build-env /out .
ENTRYPOINT [ "dotnet", "/DotNetCollectorForGitHub.dll" ]