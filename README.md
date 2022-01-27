# PostgreSQL Management 
A simple service to create a PostgreSQL database, user, and role for Infrastructure purposes.

# Example API Usage

## Create or Modify a Database
POST: https://sample.url.com:5301/database  
Authentication: Disabled  
JSON Body:
```
{
    "Host": "sample.url.com",
    "User": "user-name",
    "Password": "user-name-password",
    "NewUserPassword": "new-user-password",
    "DatabaseName": "Jaws"
}
```

## Verify a Database Exists
GET: https://sample.url.com:5301/database  
Authentication: Disabled  
JSON Body:
```
{
    "Host": "sample.url.com",
    "User": "user-name",
    "Password": "user-name-password",
    "DatabaseName": "Jaws"
}
```

## Run Query (No Results) 
POST: https://sample.url.com:5301/query  
Authentication: Disabled  
JSON Body:
```
{
    "Host": "localhost:55432",
    "User": "postgres",
    "Password": "mysecretpassword",
    "DatabaseName": "jaws",
    "QueryString": "CREATE TABLE IF NOT EXISTS public.movies(title varchar NULL, year int NULL); INSERT INTO public.movies(title, year) VALUES ('Jaws', 1975); INSERT INTO public.movies(title, year) VALUES ('Jaws 2', 1978); INSERT INTO public.movies(title, year) VALUES ('Jaws 3-D', 1983);"
}
```

## Get Data (Results as JSON)
GET: https://sample.url.com:5301/query  
Authentication: Disabled  
JSON Body:
```
{
    "Host": "localhost:55432",
    "User": "postgres",
    "Password": "mysecretpassword",
    "DatabaseName": "jaws",
    "QueryString": "SELECT * FROM public.movies;"
}
```

# Considerations

## Verifying
All `POST` commands use the `GET` methods to ensure the objects have been inserted to the database before returning `201`.  The `GET` commands are there as a courtesy and are not necessary for typical use.

## New User Password
If no password is provided for the new database user, the password for the originating user will be duplicated and used.  

## Platform
By default, the platform is self-hosted.  Currently, self-hosted and Azure are the only supported platforms values.  To specify, include an element in the JSON body named `Platform` and set the desired value.  
* Self-hosted platforms will create the new new database role as a `SUPERUSER`.  
* Azure platforms will create the role in the `azure_pg_admin` role and will ensure the new username is formatted properly when creating the database (`databasename@host`).  

_NOTE: Omitting the `Platform` object will default to self-hosted._
```
{
    "Platform": "Azure",
    "Host": "sample.postgres.database.azure.com",
    "User": "user-name@sample",
    "Password": "user-name-password",
    "NewUserPassword": "new-user-password",
    "DatabaseName": "Jaws"
}
```

## Port Number
By default, the Port is `5432`.  To change this, include an element in the JSON body named `Port` and set the desired value.  
```
{
    "Host": "sample.url.com",
    "Port": 2345,
    "User": "user-name",
    "Password": "user-name-password",
    "NewUserPassword": "new-user-password",
    "DatabaseName": "Jaws"
}
```

## SSL Mode
By default, the SSL Mode is `Prefer`.  To change this, include an element in the JSON body named `SslMode` and set the value to a valid PostgreSQL value (i.e. `Disable`, `Allow`, `Prefer`, or `Require`).  
```
{
    "Host": "sample.url.com",
    "SslMode": "Require"
    "User": "user-name",
    "Password": "user-name-password",
    "NewUserPassword": "new-user-password",
    "DatabaseName": "Jaws"
}
```

## Schemas
By default, the public schema will be secured to the new user and role, however, it might be necessary to seed additional schemas (i.e. an `admin` schema for logging).  To create new schemas, include an element in the JSON body named `Schemas` and set the value to a valid string array of the desired schema(s).  
```
{
    "Host": "sample.url.com",
    "User": "user-name",
    "Password": "user-name-password",
    "NewUserPassword": "new-user-password",
    "DatabaseName": "Jaws",
    "Schemas": ["admin","beach","boat"]
}
```

## Public Access to the Database
By default, the database will revoke public access after being created.  To change this, include an element in the JSON body named `RevokePublicAccess` and set the value to `false`.  
```
{
    "Host": "sample.url.com",
    "User": "user-name",
    "Password": "user-name-password",
    "NewUserPassword": "new-user-password",
    "DatabaseName": "Jaws",
    "RevokePublicAccess": false
}
```

## Modify an Existing Database
By default, the user, role, and database will not be modified with the provided values if the database already exist.  To change this, include an element in the JSON body named `ModifyExisting` and set the value to `true`.  
```
{
    "Host": "sample.url.com",
    "User": "user-name",
    "Password": "user-name-password",
    "NewUserPassword": "new-user-password",
    "DatabaseName": "Jaws",
    "ModifyExisting": true
}
```