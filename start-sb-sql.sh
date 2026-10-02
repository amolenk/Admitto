docker run -d \
  --name sb-sql \
  --restart unless-stopped \
  -p 1433:1433 \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='Dev!P@ssw0rd' \
  mcr.microsoft.com/mssql/server:2022-latest
