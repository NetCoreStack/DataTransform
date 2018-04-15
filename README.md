### SQL to MongoDb Transform Tool for .NET Core

### MongoDb Database (v3.6+)

    docker volume create --name=mongodata

    docker run -it -v mongodata:/data/db -p 27017:27017 -d mongo

### MSSQL Linux Database (Default MusicStore)

    docker build -t localsql .

    docker run -p 1401:1433 --name sql1 -d localsql

### Prerequisites
> [ASP.NET Core](https://github.com/aspnet/Home)