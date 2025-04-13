  //docker exec -it postgres-container psql -U root myDataBase
  //docker run --name postgres-container -e POSTGRES_USER=root -e POSTGRES_PASSWORD=myPassword -e POSTGRES_DB=myDataBase -p 5432:5432 -d postgres
  // dotnet ef migrations add InitialCreate
  //dotnet ef database update
  // dotnet ef database drop
  // SELECT * FROM public."Users";
  dotnet tool update --global dotnet-ef