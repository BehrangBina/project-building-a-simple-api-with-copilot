# Employee API

A minimal RESTful API for managing employees, built with ASP.NET Core Minimal APIs. Includes JWT authentication, Swagger UI, request validation, and logging middleware.

## Features
- CRUD operations for employees (Create, Read, Update, Delete)
- JWT authentication for secure endpoints (PUT, DELETE)
- `/login` endpoint for obtaining JWT tokens (demo credentials)
- Request validation for employee data
- Enhanced logging middleware (logs requests and responses)
- Swagger UI for interactive API documentation and testing

## Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Git (optional, for cloning)

### Setup
1. **Clone the repository** (if needed):
   ```sh
   git clone <your-repo-url>
   cd project-building-a-simple-api-with-copilot
   ```
2. **Restore dependencies:**
   ```sh
   dotnet restore
   ```
3. **Run the API:**
   ```sh
   dotnet run --project EmployeeApi.csproj
   ```
4. **Open Swagger UI:**
   Visit [http://localhost:5231](http://localhost:5231) in your browser. You will be redirected to `/swagger` for interactive API docs.

## Authentication
- **Login endpoint:** `/login`
- **Demo credentials:**
  - Username: `admin`
  - Password: `password`
- Use the `/login` endpoint to obtain a JWT token. Click the "Authorize" button in Swagger UI and paste the token as `Bearer <token>` to access protected endpoints (PUT, DELETE).

## API Endpoints

| Method | Endpoint                | Description                | Auth Required |
|--------|-------------------------|----------------------------|--------------|
| GET    | `/employees`            | Get all employees          | No           |
| GET    | `/employees/{id}`       | Get employee by ID         | No           |
| POST   | `/employees`            | Create new employee        | No           |
| PUT    | `/employees/{id}`       | Update employee            | Yes (JWT)    |
| DELETE | `/employees/{id}`       | Delete employee            | Yes (JWT)    |
| POST   | `/login`                | Get JWT token              | No           |

## Request Validation
- `Name` and `Position` are required for creating/updating employees.
- Invalid requests return HTTP 400 with an error message.

## Logging
- All requests and their response status codes are logged to the console.

## Testing
- Use the included `req.http` file with the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) in VS Code, or use Swagger UI for interactive testing.

## Notes
- The API uses an in-memory store; data will reset on restart.
- The JWT signing key is hardcoded for demo purposes. Change it and store securely for production.

---

**Enjoy building with ASP.NET Core Minimal APIs!** 
