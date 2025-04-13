# 1. Setup
- Download dotnet-ef: dotnet tool install --global dotnet-ef
- Update dotnet-ef: dotnet tool update --global dotnet-ef
# 1.1 Database and MinIO
- docker-compose up -d # Install docker
# 1.1.1 PostgresDB
- cd Docker/PostgresDB
# 1.1.2 MinIO
- cd Docker/MinIO
- localhost:9000
- user:     myrootuser
- password: myrootpassword
- Administrator/Buckets/Create Bucket
- User/Access Keys/Create Access Key 
- "accessKey":"td45L84nRQkNhXUs9THA","secretKey":"8PUR3vqC2oXqrSu2HjHLm5h6I46oA9Bk5FAgB1Ao",
# 1.2 Build and run
- dotnet ef migrations add InitialCreate
- dotnet ef database update
- dotnet ef database drop # if database broken
- dotnet run (do not build)
# 2. Api
- Read it: https://axios-http.com/docs/intro
- Add bruno file, all api setting on there
# 3. Some tips in docker/README.me 



