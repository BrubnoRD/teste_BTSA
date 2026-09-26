# API de Transferências Financeiras (.NET 8).
# Construída pelo docker-compose.yml da raiz: docker compose up -d --build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura primeiro só com o .csproj para aproveitar o cache das camadas quando apenas o código muda.
COPY TransferenciasFinanceiras.Api.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TransferenciasFinanceiras.Api.dll"]
