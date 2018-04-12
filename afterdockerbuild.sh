#!bin/bash

sudo docker exec -it sql1 /opt/mssql-tools/bin/sqlcmd -S localhost \
   -U SA -P 'P@ssword1' \
   -Q 'RESTORE FILELISTONLY FROM DISK = "/var/opt/mssql/backup/MusicStore.bak"' \
   | tr -s ' ' | cut -d ' ' -f 1-2

sudo docker exec -it sql1 /opt/mssql-tools/bin/sqlcmd \
   -S localhost -U SA -P 'P@ssword1' \
   -Q 'RESTORE DATABASE [MusicStore] FROM  DISK = "/var/opt/mssql/backup/MusicStore.bak" WITH  FILE = 1,  MOVE "MusicStore" TO "/var/opt/mssql/data/MusicStore.mdf",  MOVE "MusicStore_log" TO "/var/opt/mssql/data/MusicStore_log.ldf",  NOUNLOAD,  STATS = 5'