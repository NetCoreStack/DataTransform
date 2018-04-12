FROM microsoft/mssql-server-linux:2017-latest

RUN mkdir -p /var/opt/mssql/backup

COPY MusicStore.bak /var/opt/mssql/backup

ENV MSSQL_SA_PASSWORD=P@ssword1

ENV ACCEPT_EULA=Y