# JWT Authentication Guide - CNAB Processor

## Summary

JWT Bearer authentication is **ALWAYS ENABLED** and ready for production use.

## Current Status: REQUIRED Authentication (Always Enabled)

**ALL CNAB endpoints require authentication!** This ensures:
- Secure API access
- Production-ready authentication
- Protected file upload and data access
- Proper user tracking and authorization

**Public Endpoints (No auth required):**
- `/api/health` - Health check
- `/api/auth/login` - Login endpoint
- `/api/auth/demo-credentials` - View demo credentials

---

## Frontend Login Page

The application includes a complete login interface at **http://localhost:3000**

### Features:
- Modern, responsive login page (no CSS framework!)
- Auto-fill buttons for demo credentials
- Automatic token management
- Token expiration handling (60 minutes)
- Auto-logout when token expires
- Secure JWT storage in localStorage

### Quick Login:
1. Access **http://localhost:3000**
2. Click the **" Admin"** button (auto-fills credentials)
3. Click **"Login"**
4. You're in! Upload files and view transactions

---

## How to Use JWT Authentication via API

### Step 1: Start the Application

```bash
docker-compose up
```

Access:
- **Frontend:** http://localhost:3000 (with login page)
- **Backend API:** http://localhost:5099
- **Swagger UI:** http://localhost:5099/swagger

---

### Step 2: Login

1. **Expand the endpoint** `POST /api/auth/login`
2. **Click "Try it out"**
3. **Paste this JSON in the request body:**

```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

4. **Click "Execute"**

5. **You will receive a response like:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-11-28T13:00:00Z",
  "username": "admin"
}
```

6. ** COPY the `token` field value** (the entire long text)

---

### Step 3: Authorize in Swagger

1. **Click the "Authorize" button ** (top right corner of the page)

2. **A window will open. In the "Value" field, type:**

```
Bearer {paste-your-token-here}
```

**IMPORTANT:** Write "Bearer " (with space) before the token!

**Complete example:**
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJzdWIiOiJhZG1pbiIsImp0aSI6IjEyMzQ1Njc4IiwiaWF0IjoiMTcwMTAwMDAwMCIsImV4cCI6MTcwMTAwMzYwMCwiaXNzIjoiQ25hYlByb2Nlc3NvciIsImF1ZCI6IkNuYWJQcm9jZXNzb3JVc2VycyJ9.abcd1234...
```

3. **Click "Authorize"**

4. **Click "Close"**

5. **Done!** Now the padlock  next to each endpoint will be closed, indicating that you are authenticated.

---

### Step 4: Test Protected Endpoints

Now you can test any endpoint:

- `POST /api/cnab/upload` - Upload CNAB file
- `GET /api/cnab/transactions` - List transactions
- `GET /api/cnab/transactions/paged` - List with pagination
- `GET /api/cnab/balances` - View balances by store
- `GET /api/cnab/store/{storeName}` - Transactions for a specific store

All requests now automatically include the header:
```
Authorization: Bearer {your-token}
```

---

## Available Credentials

### Administrator User
- **Username:** `admin`
- **Password:** `Admin@123`

### Regular User
- **Username:** `user`
- **Password:** `User@123`

** Tip:** You can see the credentials at endpoint `GET /api/auth/demo-credentials` (public, does not require authentication)

---

## Testing WITHOUT Swagger (via curl)

### 1. Login
```bash
curl -X POST http://localhost:5099/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"admin\",\"password\":\"Admin@123\"}"
```

**Response:**
```json
{
  "token": "eyJhbGc...",
  "expiresAt": "2024-11-28T13:00:00Z",
  "username": "admin"
}
```

### 2. Use the Token in Requests

```bash
# Replace {TOKEN} with the value returned from login
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Upload file
curl -X POST http://localhost:5099/api/cnab/upload \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@CNAB.txt"

# List transactions
curl http://localhost:5099/api/cnab/transactions \
  -H "Authorization: Bearer $TOKEN"

# List paginated transactions
curl "http://localhost:5099/api/cnab/transactions/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

---

## What Happens Without Authentication?

If you try to access a protected endpoint without the token (or with invalid/expired token):

**Status:** `401 Unauthorized`

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "00-abc123-def456-00"
}
```

**In the Frontend:**
- You'll be automatically redirected to the login page
- Any expired tokens are cleared from localStorage
- A clean login experience is presented

---

## Always Public Endpoints (Do Not Require Authentication)

These endpoints are always public, even with authentication enabled:

- `GET /api/health` - Health check
- `POST /api/auth/login` - Login
- `GET /api/auth/demo-credentials` - View demo credentials

---

## Token Expiration

- **Duration:** 60 minutes (1 hour)
- **Configurable in:** `appsettings.json`  `JwtSettings.ExpiryMinutes`

When the token expires, you need to login again.

---

## Configuration (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJwtTokenGeneration123456!",
    "Issuer": "CnabProcessor",
    "Audience": "CnabProcessorUsers",
    "ExpiryMinutes": "60"
  },
  "DemoUsers": {
    "admin": "Admin@123",
    "user": "User@123"
  }
}
```

 **IMPORTANT:** Before going to production:
1. Change the `SecretKey` to a stronger and more secure key
2. Use environment variable or Azure Key Vault
3. Implement a real user system with database
4. Use password hashing (BCrypt/Argon2)
5. Remove the `/api/auth/demo-credentials` endpoint

---

## Troubleshooting

### Problem: "401 Unauthorized" even after login

**Solutions:**
1. Make sure to include "Bearer " before the token
2. Check if the token has not expired (60 minutes)
3. Copy the complete token (without extra spaces)
4. Logout and login again

### Problem: Swagger does not send the token automatically

**Solution:**
1. Click "Authorize" 
2. Check if the padlock is closed 
3. If not, do the authorization process again

### Problem: Token is not accepted

**Solution:**
1. Check if the application is running (has not restarted)
2. Check if the JWT configuration is correct in `appsettings.json`
3. Login again to get a new token

---

## Visual Summary

### Frontend Flow (Web UI):
```

  1. Access http://localhost:3000                            
      Login page appears automatically                      
                                                             
                                                            
                                                             
  2. Click " Admin" or " User" button                   
     (auto-fills credentials)                                
                                                             
                                                            
                                                             
  3. Click "Login"                                           
      Token automatically stored                            
                                                             
                                                            
                                                             
  4. Access main application                               
      Upload CNAB files                                    
      View transactions                                    
      See balances                                         
      Auto-logout after 60 minutes                         

```

### API Flow (Swagger/curl):
```

  1. POST /api/auth/login                                    
     { "username": "admin", "password": "Admin@123" }        
                                                             
                                                            
                                                             
  2. Response: { "token": "eyJhbGc..." }                     
     Copy the token value                                    
                                                             
                                                            
                                                             
  3. Click "Authorize"  in Swagger                         
     Paste: Bearer eyJhbGc...                               
                                                             
                                                            
                                                             
  4. Use protected endpoints                               
      Upload file                                          
      List transactions                                    
      View balances                                        

```

---





