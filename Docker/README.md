- docker exec -it postgres-container psql -U root myDataBase
- docker run --name postgres-container -e POSTGRES_USER=root -e POSTGRES_PASSWORD=myPassword -e POSTGRES_DB=myDataBase -p 5432:5432 -d postgres
- \dt # read table
- SELECT * FROM "AspNetUsers";
- docker exec -it mongodb mongosh -u user -p password --authenticationDatabase admin
- UPDATE "AspNetUsers" SET "IsPro" = false;

