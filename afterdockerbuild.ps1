docker exec -it sql1 /opt/mssql-tools/bin/sqlcmd -S localhost `
   -U SA -P "P@ssword1" `
   -Q "RESTORE FILELISTONLY FROM DISK = '/var/opt/mssql/backup/MusicStore.bak'"

docker exec -it sql1 /opt/mssql-tools/bin/sqlcmd `
   -S localhost -U SA -P "P@ssword1" `
   -Q "RESTORE DATABASE [MusicStore] FROM  DISK = N'/var/opt/mssql/backup/MusicStore.bak' WITH  FILE = 1,  MOVE N'MusicStore' TO N'/var/opt/mssql/data/MusicStore.mdf',  MOVE N'MusicStore_log' TO N'/var/opt/mssql/data/MusicStore_log.ldf',  NOUNLOAD,  STATS = 5"