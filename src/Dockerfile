ARG DOTNET_VERSION=6.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS base
WORKDIR /source
COPY PsqlManagement.API/*.csproj .
RUN dotnet restore
COPY PsqlManagement.API .


FROM base as build
WORKDIR /source
RUN dotnet build -c Release --no-restore


FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build


FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS release
RUN apk --no-cache add bash curl
RUN addgroup -g 10001 -S user && adduser -u 10001 -S user -G user
USER user
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT [ "dotnet", "PsqlManagement.API.dll" ]
