FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

RUN apt-get update && \
    apt-get install -y --no-install-recommends libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["LocalLlmAssistant.Api.csproj", "./"]

RUN dotnet restore "LocalLlmAssistant.Api.csproj"

COPY . .

RUN dotnet build \
    "LocalLlmAssistant.Api.csproj" \
    -c Release \
    -o /app/build


FROM build AS publish

RUN dotnet publish \
    "LocalLlmAssistant.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LocalLlmAssistant.Api.dll"]