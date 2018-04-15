FROM microsoft/mssql-server-linux:2017-latest

RUN mkdir -p /var/opt/mssql/backup

COPY MusicStore.bak /var/opt/mssql/backup

##Enable it if you have already downloaded the .bak file for this database
##https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorks2016.bak
#COPY AdventureWorks2016.bak /var/opt/mssql/backup

ENV MSSQL_SA_PASSWORD=P@ssword1

ENV ACCEPT_EULA=Y