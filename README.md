# User Management API - Complete Documentation

## Project Overview

The User Management API is an ASP.NET Core Web API built for TechHive Solutions to manage user records efficiently. The API supports full CRUD (Create, Read, Update, Delete) operations with comprehensive validation, error handling, and security features including logging, error handling, and token-based authentication middleware.

## Project Structure

```
UserManagementAPI/
├── Models/
│   └── User.cs                          # User entity model
├── Data/
│   └── ApplicationDbContext.cs           # Entity Framework DbContext
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs        # Global exception handling
│   ├── AuthenticationMiddleware.cs       # Token-based authentication
│   └── LoggingMiddleware.cs              # Request/response logging
├── Migrations/
│   └── [Database migrations]
├── Program.cs                           # API configuration and endpoints
├── appsettings.json                     # Configuration settings
└── UserManagementAPI.csproj            # Project file
```

## Technologies Used

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server (LocalDB)
- **ORM**: Entity Framework Core 8.0
- **Authentication**: Token-based (Bearer token)
- **Logging**: Built-in ILogger interface

## API Endpoints

### 1. Get All Users

- **Method**: `GET`
- **Endpoint**: `/api/users`
- **Headers**: `Authorization: Bearer <token>`
- **Response**: 200 OK

```json
[
  {
    "id": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phoneNumber": "555-1234",
    "department": "IT",
    "createdAt": "2026-01-29T13:58:25.000Z",
    "updatedAt": null
  }
]
```

### 2. Get User by ID

- **Method**: `GET`
- **Endpoint**: `/api/users/{id}`
- **Headers**: `Authorization: Bearer <token>`
- **Parameters**: `id` (integer) - User ID
- **Response**: 200 OK or 404 Not Found

### 3. Create New User

- **Method**: `POST`
- **Endpoint**: `/api/users`
- **Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
- **Request Body**:

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "555-1234",
  "department": "IT"
}
```

- **Validations**:
  - FirstName and LastName: Required, max 100 characters
  - Email: Required, must be valid format, unique
  - PhoneNumber: Optional, max 20 characters
  - Department: Optional, max 100 characters
- **Response**: 201 Created or 400 Bad Request

### 4. Update User

- **Method**: `PUT`
- **Endpoint**: `/api/users/{id}`
- **Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
- **Parameters**: `id` (integer) - User ID
- **Request Body**: Same as Create
- **Validations**: Same as Create (with duplicate email check excluding current user)
- **Response**: 200 OK, 404 Not Found, or 400 Bad Request

### 5. Delete User

- **Method**: `DELETE`
- **Endpoint**: `/api/users/{id}`
- **Headers**: `Authorization: Bearer <token>`
- **Parameters**: `id` (integer) - User ID
- **Response**: 200 OK with success message or 404 Not Found

## Features

### 1. Input Validation

All endpoints include comprehensive validation:

- Required field checks (FirstName, LastName, Email)
- Email format validation using MailAddress parser
- Email uniqueness validation
- Maximum length validation for all string fields
- Null/whitespace checks

### 2. Error Handling

The API includes a global error handling middleware that:

- Catches all unhandled exceptions
- Returns consistent JSON error responses
- Includes error details and messages
- Logs all exceptions for debugging

**Error Response Format**:

```json
{
  "error": "Internal server error.",
  "details": "Exception message"
}
```

### 3. Logging Middleware

The logging middleware tracks:

- HTTP method (GET, POST, PUT, DELETE)
- Request path and client IP address
- Response status code
- Request processing duration in milliseconds

**Log Output Example**:

```
Incoming Request: POST /api/users from 127.0.0.1
Outgoing Response: POST /api/users => 201 (Duration: 45ms)
```

### 4. Authentication Middleware

The authentication middleware:

- Validates Bearer tokens on all protected endpoints
- Skips authentication for Swagger UI endpoints
- Returns 401 Unauthorized for missing/invalid tokens
- Validates token format and length (minimum 10 characters)

**Token Validation Requirements**:

- Format: `Bearer <token>`
- Token length: Minimum 10 characters
- Token must not be empty or whitespace

**Authentication Error Response**:

```json
{
  "error": "Missing or invalid authorization token."
}
```

### 5. Middleware Pipeline Order

The middleware is configured in the correct order:

1. **ErrorHandlingMiddleware** - First (catches all exceptions)
2. **AuthenticationMiddleware** - Second (validates tokens)
3. **LoggingMiddleware** - Last (logs requests/responses)

This order ensures:

- Exceptions are handled before logging
- Authentication is validated before logging sensitive data
- All requests are logged after processing

## Database Schema

### Users Table

```sql
CREATE TABLE [Users] (
  [Id] int PRIMARY KEY IDENTITY,
  [FirstName] nvarchar(100) NOT NULL,
  [LastName] nvarchar(100) NOT NULL,
  [Email] nvarchar(256) NOT NULL UNIQUE,
  [PhoneNumber] nvarchar(20),
  [Department] nvarchar(100),
  [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
  [UpdatedAt] datetime2 NULL
)
```

## Configuration

### Connection String

Located in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=UserManagementDB;Trusted_Connection=true;"
  }
}
```

### Logging Level

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Running the Application

### Prerequisites

- .NET 8.0 SDK
- SQL Server LocalDB (or any SQL Server instance)
- Postman or similar API testing tool

### Setup Steps

1. **Navigate to project directory**:

```bash
cd c:\Users\user\OneDrive\Desktop\UserManagmentApp\UserManagementAPI
```

2. **Build the project**:

```bash
dotnet build
```

3. **Apply database migrations** (if needed):

```bash
dotnet ef database update
```

4. **Run the application**:

```bash
dotnet run
```

5. **Access the API**:

- API Base URL: `http://localhost:5257`
- Swagger UI: `http://localhost:5257/swagger`

## Testing the API

### Using Postman

#### 1. Set up Environment Variables

Create a Postman Environment with:

- `base_url`: `http://localhost:5257`
- `token`: `your-test-token-at-least-10-characters`

#### 2. Test Create User (POST)

```
POST http://localhost:5257/api/users
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
  Content-Type: application/json

Body:
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "555-1234",
  "department": "IT"
}
```

**Expected Response**: 201 Created

#### 3. Test Get All Users (GET)

```
GET http://localhost:5257/api/users
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
```

**Expected Response**: 200 OK with user array

#### 4. Test Get User by ID (GET)

```
GET http://localhost:5257/api/users/1
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
```

**Expected Response**: 200 OK with user object

#### 5. Test Update User (PUT)

```
PUT http://localhost:5257/api/users/1
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
  Content-Type: application/json

Body:
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phoneNumber": "555-5678",
  "department": "HR"
}
```

**Expected Response**: 200 OK with updated user

#### 6. Test Delete User (DELETE)

```
DELETE http://localhost:5257/api/users/1
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
```

**Expected Response**: 200 OK with success message

### Testing Edge Cases

#### 1. Missing Required Fields

```
POST http://localhost:5257/api/users
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
  Content-Type: application/json

Body:
{
  "firstName": "John"
}
```

**Expected Response**: 400 Bad Request - "FirstName and LastName are required."

#### 2. Invalid Email Format

```
POST http://localhost:5257/api/users
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
  Content-Type: application/json

Body:
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "invalid-email"
}
```

**Expected Response**: 400 Bad Request - "Email format is invalid."

#### 3. Duplicate Email

```
POST http://localhost:5257/api/users
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
  Content-Type: application/json

Body:
{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "john.doe@example.com"
}
```

**Expected Response**: 400 Bad Request - "Email is already in use."

#### 4. Non-existent User ID

```
GET http://localhost:5257/api/users/999
Headers:
  Authorization: Bearer your-test-token-at-least-10-characters
```

**Expected Response**: 404 Not Found - "User with ID 999 not found."

#### 5. Missing Authorization Header

```
GET http://localhost:5257/api/users
```

**Expected Response**: 401 Unauthorized - "Missing or invalid authorization token."

#### 6. Invalid Token Format

```
GET http://localhost:5257/api/users
Headers:
  Authorization: invalid-token
```

**Expected Response**: 401 Unauthorized - "Invalid token format. Use 'Bearer <token>'."

#### 7. Invalid Token (Too Short)

```
GET http://localhost:5257/api/users
Headers:
  Authorization: Bearer short
```

**Expected Response**: 401 Unauthorized - "Invalid token."

## How Microsoft Copilot Assisted in Development

1. **Project Scaffolding**: Copilot helped generate the initial project structure and configuration in Program.cs, including service registration and middleware setup.

2. **Database Design**: Copilot assisted in designing the User model and Entity Framework DbContext with proper entity configuration and constraints.

3. **CRUD Endpoints**: Copilot generated the complete CRUD endpoint implementations with proper parameter handling and response types.

4. **Input Validation**: Copilot helped implement comprehensive validation logic for email format, uniqueness checks, and required field validation.

5. **Error Handling**: Copilot guided the creation of the ErrorHandlingMiddleware to catch and handle unhandled exceptions globally.

6. **Middleware Development**: Copilot assisted in writing the AuthenticationMiddleware and LoggingMiddleware with proper HTTP context manipulation.

7. **Middleware Pipeline Configuration**: Copilot helped determine the correct order of middleware execution for optimal performance and security.

8. **Email Validation**: Copilot provided the email validation helper function using the MailAddress class.

9. **Documentation**: Copilot helped structure and generate comprehensive API documentation with examples and testing procedures.

## Troubleshooting

### Database Connection Issues

If you receive a connection error:

1. Ensure SQL Server LocalDB is running: `sqllocaldb start mssqllocaldb`
2. Verify the connection string in `appsettings.json`
3. Check if the database exists: `sqllocaldb info mssqllocaldb`

### Port Already in Use

If port 5257 is already in use:

1. Modify the port in `Properties/launchSettings.json`
2. Or find and kill the process using the port

### Swagger UI Not Loading

- Swagger is only available in Development environment
- Check `app.Environment.IsDevelopment()` condition in Program.cs

## Future Enhancements

- JWT token generation and validation
- Role-based authorization (Admin, User)
- Pagination for large user lists
- Advanced search and filtering
- Audit logging for data changes
- Rate limiting
- HTTPS enforcement in production
