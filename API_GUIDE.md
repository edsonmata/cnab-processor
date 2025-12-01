# 📡 CNAB Processor - API Guide

Complete guide for consuming the CNAB Processor REST API.

---

## 🌐 Base URL

```
http://localhost:5099/api
```

---

## 📋 Table of Contents

1. [Authentication](#authentication)
2. [Endpoints Overview](#endpoints-overview)
3. [Detailed Endpoint Documentation](#detailed-endpoint-documentation)
4. [Request/Response Examples](#requestresponse-examples)
5. [Error Handling](#error-handling)
6. [Code Examples](#code-examples)
7. [Postman Collection](#postman-collection)

---

## 🔐 Authentication

**JWT Bearer authentication is REQUIRED** for all endpoints (except `/api/health` and `/api/auth/*`).

### Demo Credentials

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Administrator |
| `user` | `User@123` | User |

### How to Authenticate

1. **Get a token** by logging in:
```bash
curl -X POST http://localhost:5099/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```

Response:
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "expiresAt": "2024-11-28T16:30:45.123Z"
}
```

2. **Include the token** in the `Authorization` header for all subsequent requests:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

3. **Token expires** after 60 minutes. You'll receive a `401 Unauthorized` response when the token expires.

### Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/auth/login` | Login and get JWT token |
| `GET` | `/api/auth/me` | Get current user info (requires auth) |
| `GET` | `/api/auth/demo-credentials` | View demo credentials |

---

## 📊 Endpoints Overview

### Authentication Endpoints (No Auth Required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check |
| `POST` | `/api/auth/login` | Login and get JWT token |
| `GET` | `/api/auth/demo-credentials` | View demo credentials |

### CNAB Endpoints (Auth Required 🔒)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/cnab/upload` | Upload CNAB file |
| `GET` | `/api/cnab/transactions` | Get all transactions |
| `GET` | `/api/cnab/store/{storeName}` | Get transactions by store |
| `GET` | `/api/cnab/balances` | Get store balances |
| `GET` | `/api/cnab/stats` | Get system statistics |
| `GET` | `/api/auth/me` | Get current user info |

---

## 📖 Detailed Endpoint Documentation

### Authentication Endpoints

#### 1. Login

Authenticate and receive a JWT token.

**Endpoint:**
```http
POST /api/auth/login
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImp0aSI6IjEyMzQ1Njc4LTkwYWItY2RlZi0xMjM0LTU2Nzg5MGFiY2RlZiIsImV4cCI6MTcwMTI3MDY0NSwiaXNzIjoiQ25hYlByb2Nlc3NvckFwaSIsImF1ZCI6IkNuYWJQcm9jZXNzb3JDbGllbnQifQ.AbCdEfGhIjKlMnOpQrStUvWxYz123456789",
  "username": "admin",
  "expiresAt": "2024-11-28T16:30:45.123Z"
}
```

**Response (401 Unauthorized) - Invalid credentials:**
```json
{
  "success": false,
  "message": "Invalid username or password"
}
```

**Response (400 Bad Request) - Missing fields:**
```json
{
  "success": false,
  "message": "Username and password are required"
}
```

---

#### 2. Get Current User

Get information about the currently authenticated user.

**Endpoint:**
```http
GET /api/auth/me
```

**Headers:**
```
Authorization: Bearer {your-jwt-token}
```

**Response (200 OK):**
```json
{
  "username": "admin",
  "isAuthenticated": true
}
```

**Response (401 Unauthorized) - No token or invalid token:**
```json
{
  "message": "Unauthorized"
}
```

---

#### 3. Get Demo Credentials

View available demo credentials for testing.

**Endpoint:**
```http
GET /api/auth/demo-credentials
```

**Headers:**
```
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "message": "Demo credentials for testing",
  "credentials": [
    {
      "username": "admin",
      "password": "Admin@123",
      "description": "Administrator account"
    },
    {
      "username": "user",
      "password": "User@123",
      "description": "Regular user account"
    }
  ]
}
```

---

### CNAB Endpoints

### 1. Health Check

Check if the API is running and healthy.

**Endpoint:**
```http
GET /api/health
```

**Headers:**
```
Content-Type: application/json
```

**Response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2024-11-28T15:30:45.123Z"
}
```

**Use Case:**
- Monitor API availability
- Load balancer health checks
- Container orchestration health probes

---

### 2. Upload CNAB File

Upload and process a CNAB transaction file.

**🔒 Authentication Required**

**Endpoint:**
```http
POST /api/cnab/upload
```

**Headers:**
```
Authorization: Bearer {your-jwt-token}
Content-Type: multipart/form-data
```

**Request Body:**
- **Field name:** `file`
- **Type:** File
- **Format:** `.txt` (CNAB fixed-width format)
- **Max size:** No limit defined (recommended: 10MB)

**CNAB File Format:**
Each line must be exactly 81 characters:
```
[Type:1][Date:8][Amount:10][CPF:11][Card:12][Time:6][Owner:14][Store:19]
```

**Example CNAB File Content:**
```
3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       
5201903010000013200556418150633648****0099153453MARIA SILVA   MERCEARIA 3 IRMÃOS
2201903010000012200845152540736777****1313172712MARCOS PEREIRA LOJA DO Ó - MATRIZ
```

**Response (200 OK):**
```json
{
  "success": true,
  "transactionCount": 3,
  "message": "Successfully imported 3 transactions!",
  "fileName": "CNAB.txt"
}
```

**Response (400 Bad Request) - No file:**
```json
{
  "success": false,
  "transactionCount": 0,
  "message": "No file was uploaded.",
  "fileName": ""
}
```

**Response (400 Bad Request) - Invalid format:**
```json
{
  "success": false,
  "transactionCount": 0,
  "message": "Invalid CNAB file format.",
  "fileName": "invalid.txt"
}
```

**Response (500 Internal Server Error):**
```json
{
  "success": false,
  "transactionCount": 0,
  "message": "Error processing file: [error details]",
  "fileName": "CNAB.txt"
}
```

---

### 3. Get All Transactions

Retrieve all imported transactions, ordered by date (descending).

**🔒 Authentication Required**

**Endpoint:**
```http
GET /api/cnab/transactions
```

**Headers:**
```
Authorization: Bearer {your-jwt-token}
Accept: application/json
```

**Query Parameters:**
None

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "type": "3",
    "typeDescription": "Financiamento",
    "nature": "Expense",
    "date": "2019-03-01T00:00:00",
    "time": "14:13:58",
    "amount": 142.00,
    "signedAmount": -142.00,
    "cpf": "09620676017",
    "cardNumber": "4753****3153",
    "storeOwner": "JOÃO MACEDO",
    "storeName": "BAR DO JOÃO"
  },
  {
    "id": 2,
    "type": "1",
    "typeDescription": "Débito",
    "nature": "Income",
    "date": "2019-03-01T00:00:00",
    "time": "15:30:00",
    "amount": 250.00,
    "signedAmount": 250.00,
    "cpf": "12345678901",
    "cardNumber": "5432****8765",
    "storeOwner": "MARIA SILVA",
    "storeName": "MERCEARIA 3 IRMÃOS"
  }
]
```

**Response (200 OK) - Empty:**
```json
[]
```

**Field Descriptions:**

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Unique transaction identifier |
| `type` | string | Transaction type code (1-9) |
| `typeDescription` | string | Human-readable type in Portuguese |
| `nature` | string | "Income" or "Expense" |
| `date` | datetime | Transaction date (ISO 8601) |
| `time` | string | Transaction time (HH:mm:ss) |
| `amount` | decimal | Transaction amount (always positive) |
| `signedAmount` | decimal | Amount with sign (+/-) |
| `cpf` | string | Beneficiary's tax ID (11 digits) |
| `cardNumber` | string | Masked card number |
| `storeOwner` | string | Store owner/representative name |
| `storeName` | string | Store name |

---

### 4. Get Transactions by Store

Retrieve all transactions for a specific store.

**🔒 Authentication Required**

**Endpoint:**
```http
GET /api/cnab/store/{storeName}
```

**Headers:**
```
Authorization: Bearer {your-jwt-token}
Accept: application/json
```

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `storeName` | string | Yes | Store name (URL encoded) |

**Example:**
```
/api/cnab/store/BAR%20DO%20JO%C3%83O
```

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "type": "1",
    "typeDescription": "Débito",
    "nature": "Income",
    "date": "2019-03-01T00:00:00",
    "time": "14:13:58",
    "amount": 142.00,
    "signedAmount": 142.00,
    "cpf": "09620676017",
    "cardNumber": "4753****3153",
    "storeOwner": "JOÃO MACEDO",
    "storeName": "BAR DO JOÃO"
  }
]
```

**Response (200 OK) - Store not found:**
```json
[]
```

---

### 5. Get Store Balances

Retrieve all stores with their calculated balances and transaction lists.

**🔒 Authentication Required**

**Endpoint:**
```http
GET /api/cnab/balances
```

**Headers:**
```
Authorization: Bearer {your-jwt-token}
Accept: application/json
```

**Response (200 OK):**
```json
[
  {
    "storeName": "BAR DO JOÃO",
    "totalBalance": 350.00,
    "totalIncome": 500.00,
    "totalExpenses": 150.00,
    "transactionCount": 8,
    "transactions": [
      {
        "id": 1,
        "type": "1",
        "typeDescription": "Débito",
        "nature": "Income",
        "date": "2019-03-01T00:00:00",
        "time": "14:13:58",
        "amount": 142.00,
        "signedAmount": 142.00,
        "cpf": "09620676017",
        "cardNumber": "4753****3153",
        "storeOwner": "JOÃO MACEDO",
        "storeName": "BAR DO JOÃO"
      }
    ]
  },
  {
    "storeName": "MERCEARIA 3 IRMÃOS",
    "totalBalance": 245.00,
    "totalIncome": 450.00,
    "totalExpenses": 205.00,
    "transactionCount": 6,
    "transactions": [...]
  }
]
```

**Field Descriptions:**

| Field | Type | Description |
|-------|------|-------------|
| `storeName` | string | Store name |
| `totalBalance` | decimal | Total balance (income - expenses) |
| `totalIncome` | decimal | Sum of all income transactions |
| `totalExpenses` | decimal | Sum of all expense transactions |
| `transactionCount` | integer | Number of transactions |
| `transactions` | array | List of all store transactions |

**Balance Calculation:**
```
totalBalance = totalIncome - totalExpenses
```

---

### 6. Get System Statistics

Retrieve overall system statistics.

**🔒 Authentication Required**

**Endpoint:**
```http
GET /api/cnab/stats
```

**Headers:**
```
Authorization: Bearer {your-jwt-token}
Accept: application/json
```

**Response (200 OK):**
```json
{
  "totalTransactions": 21,
  "totalStores": 4,
  "totalBalance": 1450.50,
  "biggestStore": "BAR DO JOÃO",
  "smallestStore": "LOJA DO Ó - FILIAL"
}
```

**Field Descriptions:**

| Field | Type | Description |
|-------|------|-------------|
| `totalTransactions` | integer | Total number of transactions |
| `totalStores` | integer | Total number of unique stores |
| `totalBalance` | decimal | Sum of all store balances |
| `biggestStore` | string | Store with highest balance |
| `smallestStore` | string | Store with lowest balance |

**Response (200 OK) - No data:**
```json
{
  "totalTransactions": 0,
  "totalStores": 0,
  "totalBalance": 0,
  "biggestStore": null,
  "smallestStore": null
}
```

---

### 7. Get All Transactions (Paginated) ⭐ NEW

Retrieve all transactions with pagination support. Ideal for handling large datasets efficiently.

**🔒 Authentication Required**

**Endpoint:**
```http
GET /api/cnab/transactions/paged?pageNumber=1&pageSize=10
```

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | integer | No | 1 | Page number (1-based indexing) |
| `pageSize` | integer | No | 10 | Number of items per page (max 100) |

**Example Requests:**
```bash
# Get first page with 10 items
GET /api/cnab/transactions/paged?pageNumber=1&pageSize=10

# Get page 5 with 25 items per page
GET /api/cnab/transactions/paged?pageNumber=5&pageSize=25

# Get last page
GET /api/cnab/transactions/paged?pageNumber=999&pageSize=10  # Auto-adjusts to last page
```

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "type": "3",
      "typeDescription": "Financiamento",
      "nature": "Expense",
      "date": "2019-03-01T00:00:00",
      "time": "14:13:58",
      "amount": 142.00,
      "signedAmount": -142.00,
      "cpf": "09620676017",
      "cardNumber": "4753****3153",
      "storeOwner": "JOÃO MACEDO",
      "storeName": "BAR DO JOÃO"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 100,
  "totalPages": 10,
  "hasNext": true,
  "hasPrevious": false
}
```

**Field Descriptions:**

| Field | Type | Description |
|-------|------|-------------|
| `items` | array | List of transactions on the current page |
| `pageNumber` | integer | Current page number |
| `pageSize` | integer | Number of items per page |
| `totalCount` | integer | Total transactions across all pages |
| `totalPages` | integer | Total number of pages |
| `hasNext` | boolean | Whether there's a next page |
| `hasPrevious` | boolean | Whether there's a previous page |

---

### 8. Get Store Transactions (Paginated) ⭐ NEW

Retrieve paginated transactions for a specific store.

**🔒 Authentication Required**

**Endpoint:**
```http
GET /api/cnab/store/{storeName}/paged?pageNumber=1&pageSize=10
```

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `storeName` | string | Yes | Store name (URL encoded) |

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | integer | No | 1 | Page number (1-based indexing) |
| `pageSize` | integer | No | 10 | Number of items per page (max 100) |

**Example Requests:**
```bash
# Get first page of "BAR DO JOÃO" with 10 items
GET /api/cnab/store/BAR%20DO%20JO%C3%83O/paged?pageNumber=1&pageSize=10

# Get page 2 with 25 items per page
GET /api/cnab/store/MERCEARIA%203%20IRM%C3%83OS/paged?pageNumber=2&pageSize=25

# Store with special characters (URL encoded)
GET /api/cnab/store/CAF%C3%89%20DO%20JOS%C3%89/paged?pageNumber=1&pageSize=10
```

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "type": "1",
      "typeDescription": "Débito",
      "nature": "Income",
      "date": "2019-03-01T00:00:00",
      "time": "14:13:58",
      "amount": 142.00,
      "signedAmount": 142.00,
      "cpf": "09620676017",
      "cardNumber": "4753****3153",
      "storeOwner": "JOÃO MACEDO",
      "storeName": "BAR DO JOÃO"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5,
  "hasNext": true,
  "hasPrevious": false
}
```

**Response (200 OK) - Non-existent store:**
```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0,
  "hasNext": false,
  "hasPrevious": false
}
```

**Pagination Behavior:**

- **Invalid page number** (< 1): Redirects to page 1
- **Page out of range** (> totalPages): Redirects to last page
- **Invalid page size** (< 1 or > 100): Clamps to default (10) or max (100)

---

## 🔧 Request/Response Examples

### cURL Examples

#### Step 1: Login and Get Token
```bash
# Login to get JWT token
curl -X POST http://localhost:5099/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'

# Response will contain: {"success":true,"token":"eyJhbGc...","username":"admin","expiresAt":"..."}
# Copy the token value for use in subsequent requests
```

#### Step 2: Use Token in Requests

**Set token as variable (recommended):**
```bash
# Export token as environment variable
export TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Or for Windows PowerShell:
$TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

#### Upload File
```bash
curl -X POST http://localhost:5099/api/cnab/upload \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@CNAB.txt"
```

#### Get All Transactions
```bash
curl -X GET http://localhost:5099/api/cnab/transactions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json"
```

#### Get Transactions by Store
```bash
curl -X GET "http://localhost:5099/api/cnab/store/BAR%20DO%20JO%C3%83O" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json"
```

#### Get Balances
```bash
curl -X GET http://localhost:5099/api/cnab/balances \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json"
```

#### Get Statistics
```bash
curl -X GET http://localhost:5099/api/cnab/stats \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json"
```

---

### JavaScript (Fetch API) Examples

#### Step 1: Login and Get Token
```javascript
async function login(username, password) {
  const response = await fetch('http://localhost:5099/api/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ username, password })
  });

  const data = await response.json();

  if (data.success) {
    // Store token in localStorage
    localStorage.setItem('token', data.token);
    localStorage.setItem('tokenExpiry', data.expiresAt);
    console.log('Login successful!');
    return data.token;
  } else {
    throw new Error('Login failed: ' + data.message);
  }
}

// Usage:
await login('admin', 'Admin@123');
```

#### Step 2: Helper Function to Get Token
```javascript
function getAuthHeaders() {
  const token = localStorage.getItem('token');
  if (!token) {
    throw new Error('Not authenticated. Please login first.');
  }
  return {
    'Authorization': `Bearer ${token}`
  };
}
```

#### Upload File
```javascript
async function uploadCnabFile(file) {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch('http://localhost:5099/api/cnab/upload', {
    method: 'POST',
    headers: getAuthHeaders(),
    body: formData
  });

  const result = await response.json();
  console.log(result);
}
```

#### Get All Transactions
```javascript
async function getAllTransactions() {
  const response = await fetch('http://localhost:5099/api/cnab/transactions', {
    headers: getAuthHeaders()
  });

  const transactions = await response.json();
  console.log(transactions);
}
```

#### Get Balances
```javascript
async function getStoreBalances() {
  const response = await fetch('http://localhost:5099/api/cnab/balances', {
    headers: getAuthHeaders()
  });

  const balances = await response.json();

  balances.forEach(store => {
    console.log(`${store.storeName}: R$ ${store.totalBalance.toFixed(2)}`);
  });
}
```

#### Complete Example with Error Handling
```javascript
async function completeCnabWorkflow() {
  try {
    // 1. Login
    await login('admin', 'Admin@123');

    // 2. Upload file
    const fileInput = document.querySelector('input[type="file"]');
    const file = fileInput.files[0];
    await uploadCnabFile(file);

    // 3. Get balances
    await getStoreBalances();
  } catch (error) {
    if (error.response?.status === 401) {
      console.error('Authentication failed. Please login again.');
      await login('admin', 'Admin@123');
    } else {
      console.error('Error:', error.message);
    }
  }
}
```

---

### Python Examples

#### Step 1: Login and Get Token
```python
import requests

def login(username, password):
    """Login and return JWT token"""
    url = "http://localhost:5099/api/auth/login"
    payload = {
        "username": username,
        "password": password
    }

    response = requests.post(url, json=payload)
    data = response.json()

    if data.get('success'):
        print('Login successful!')
        return data['token']
    else:
        raise Exception(f"Login failed: {data.get('message')}")

# Get token
token = login('admin', 'Admin@123')
```

#### Step 2: Create Headers with Token
```python
def get_auth_headers(token):
    """Return headers with Authorization token"""
    return {
        'Authorization': f'Bearer {token}'
    }
```

#### Upload File
```python
import requests

def upload_cnab_file(token, file_path):
    url = "http://localhost:5099/api/cnab/upload"
    headers = get_auth_headers(token)
    files = {'file': open(file_path, 'rb')}

    response = requests.post(url, headers=headers, files=files)
    print(response.json())

# Usage
upload_cnab_file(token, 'CNAB.txt')
```

#### Get All Transactions
```python
import requests

def get_all_transactions(token):
    url = "http://localhost:5099/api/cnab/transactions"
    headers = get_auth_headers(token)
    response = requests.get(url, headers=headers)

    transactions = response.json()
    print(f"Total transactions: {len(transactions)}")
    return transactions

# Usage
transactions = get_all_transactions(token)
```

#### Get Store Balances
```python
import requests

def get_store_balances(token):
    url = "http://localhost:5099/api/cnab/balances"
    headers = get_auth_headers(token)
    response = requests.get(url, headers=headers)

    balances = response.json()
    for store in balances:
        print(f"{store['storeName']}: R$ {store['totalBalance']:.2f}")
    return balances

# Usage
balances = get_store_balances(token)
```

#### Complete Example with Error Handling
```python
import requests
from typing import Optional

class CnabApiClient:
    def __init__(self, base_url: str = "http://localhost:5099/api"):
        self.base_url = base_url
        self.token: Optional[str] = None

    def login(self, username: str, password: str) -> bool:
        """Login and store token"""
        url = f"{self.base_url}/auth/login"
        response = requests.post(url, json={
            "username": username,
            "password": password
        })

        data = response.json()
        if data.get('success'):
            self.token = data['token']
            print(f"Logged in as {data['username']}")
            return True
        return False

    def _get_headers(self) -> dict:
        """Get headers with auth token"""
        if not self.token:
            raise Exception("Not authenticated. Call login() first.")
        return {'Authorization': f'Bearer {self.token}'}

    def upload_file(self, file_path: str) -> dict:
        """Upload CNAB file"""
        url = f"{self.base_url}/cnab/upload"
        files = {'file': open(file_path, 'rb')}
        response = requests.post(
            url,
            headers=self._get_headers(),
            files=files
        )
        return response.json()

    def get_balances(self) -> list:
        """Get store balances"""
        url = f"{self.base_url}/cnab/balances"
        response = requests.get(url, headers=self._get_headers())
        return response.json()

# Usage
client = CnabApiClient()
client.login('admin', 'Admin@123')
result = client.upload_file('CNAB.txt')
balances = client.get_balances()
for store in balances:
    print(f"{store['storeName']}: R$ {store['totalBalance']:.2f}")
```

---

### C# Examples

#### Step 1: Login and Get Token
```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string Username { get; set; }
    public DateTime ExpiresAt { get; set; }
}

async Task<string> LoginAsync(string username, string password)
{
    using var client = new HttpClient();
    var request = new LoginRequest
    {
        Username = username,
        Password = password
    };

    var response = await client.PostAsJsonAsync(
        "http://localhost:5099/api/auth/login",
        request
    );

    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

    if (result?.Success == true)
    {
        Console.WriteLine($"Logged in as {result.Username}");
        return result.Token;
    }

    throw new Exception("Login failed");
}

// Usage
var token = await LoginAsync("admin", "Admin@123");
```

#### Step 2: Create HttpClient with Auth Header
```csharp
HttpClient CreateAuthenticatedClient(string token)
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    return client;
}
```

#### Upload File
```csharp
using System.Net.Http.Headers;

async Task UploadCnabFileAsync(string token, string filePath)
{
    using var client = CreateAuthenticatedClient(token);
    using var form = new MultipartFormDataContent();
    using var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));

    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
    form.Add(fileContent, "file", Path.GetFileName(filePath));

    var response = await client.PostAsync(
        "http://localhost:5099/api/cnab/upload",
        form
    );

    var result = await response.Content.ReadAsStringAsync();
    Console.WriteLine(result);
}

// Usage
await UploadCnabFileAsync(token, "CNAB.txt");
```

#### Get All Transactions
```csharp
using System.Text.Json;

public class Transaction
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string TypeDescription { get; set; }
    public string Nature { get; set; }
    public DateTime Date { get; set; }
    public string Time { get; set; }
    public decimal Amount { get; set; }
    public decimal SignedAmount { get; set; }
    public string Cpf { get; set; }
    public string CardNumber { get; set; }
    public string StoreOwner { get; set; }
    public string StoreName { get; set; }
}

async Task<List<Transaction>> GetAllTransactionsAsync(string token)
{
    using var client = CreateAuthenticatedClient(token);

    var response = await client.GetAsync(
        "http://localhost:5099/api/cnab/transactions"
    );

    var json = await response.Content.ReadAsStringAsync();
    var transactions = JsonSerializer.Deserialize<List<Transaction>>(json);

    Console.WriteLine($"Total: {transactions.Count}");
    return transactions;
}

// Usage
var transactions = await GetAllTransactionsAsync(token);
```

#### Complete Example with Reusable Client
```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public class CnabApiClient : IDisposable
{
    private readonly HttpClient _client;
    private string _token;

    public CnabApiClient(string baseUrl = "http://localhost:5099/api")
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var request = new { username, password };
        var response = await _client.PostAsJsonAsync("/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result?.Success == true)
        {
            _token = result.Token;
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
            Console.WriteLine($"Logged in as {result.Username}");
            return true;
        }

        return false;
    }

    public async Task<UploadResponse> UploadFileAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));

        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _client.PostAsync("/cnab/upload", form);
        return await response.Content.ReadFromJsonAsync<UploadResponse>();
    }

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        var response = await _client.GetAsync("/cnab/transactions");
        return await response.Content.ReadFromJsonAsync<List<Transaction>>();
    }

    public async Task<List<StoreBalance>> GetBalancesAsync()
    {
        var response = await _client.GetAsync("/cnab/balances");
        return await response.Content.ReadFromJsonAsync<List<StoreBalance>>();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

// Usage
using var client = new CnabApiClient();
await client.LoginAsync("admin", "Admin@123");

var uploadResult = await client.UploadFileAsync("CNAB.txt");
Console.WriteLine($"Uploaded {uploadResult.TransactionCount} transactions");

var balances = await client.GetBalancesAsync();
foreach (var store in balances)
{
    Console.WriteLine($"{store.StoreName}: R$ {store.TotalBalance:F2}");
}
```

---

## ❌ Error Handling

### HTTP Status Codes

| Code | Description | When it occurs |
|------|-------------|----------------|
| `200 OK` | Success | Request completed successfully |
| `400 Bad Request` | Invalid request | Invalid file format, missing file, invalid credentials |
| `401 Unauthorized` | Authentication required | Missing token, invalid token, expired token |
| `404 Not Found` | Resource not found | Invalid endpoint |
| `500 Internal Server Error` | Server error | Database error, unexpected exception |

### Error Response Format

```json
{
  "success": false,
  "message": "Error description here",
  "transactionCount": 0,
  "fileName": "file.txt"
}
```

### Common Errors

#### 401 Unauthorized - Missing or Invalid Token
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "00-abc123..."
}
```

**Solutions:**
- Login again to get a new token
- Check token is included in `Authorization: Bearer {token}` header
- Verify token hasn't expired (60 minute expiry)

#### 400 Bad Request - Invalid Credentials
```json
{
  "success": false,
  "message": "Invalid username or password"
}
```

#### 400 Bad Request - Invalid CNAB Format
```json
{
  "success": false,
  "message": "Invalid CNAB file format.",
  "transactionCount": 0,
  "fileName": "invalid.txt"
}
```

#### 400 Bad Request - No File Uploaded
```json
{
  "success": false,
  "message": "No file was uploaded.",
  "transactionCount": 0,
  "fileName": ""
}
```

#### 500 Internal Server Error - Database Connection Error
```json
{
  "success": false,
  "message": "Error processing file: A network-related error occurred...",
  "transactionCount": 0,
  "fileName": "CNAB.txt"
}
```

---

## 📦 Postman Collection

### Import Collection

1. Open Postman
2. Click **Import**
3. Copy and paste the JSON below:

```json
{
  "info": {
    "name": "CNAB Processor API",
    "description": "Complete API collection for CNAB Processor with JWT Authentication",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Authentication",
      "item": [
        {
          "name": "Login",
          "event": [
            {
              "listen": "test",
              "script": {
                "exec": [
                  "var jsonData = pm.response.json();",
                  "if (jsonData.success && jsonData.token) {",
                  "    pm.collectionVariables.set('token', jsonData.token);",
                  "    console.log('Token saved:', jsonData.token);",
                  "}"
                ]
              }
            }
          ],
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"username\": \"admin\",\n  \"password\": \"Admin@123\"\n}"
            },
            "url": {
              "raw": "{{base_url}}/auth/login",
              "host": ["{{base_url}}"],
              "path": ["auth", "login"]
            },
            "description": "Login with credentials and automatically save token to collection variable"
          }
        },
        {
          "name": "Get Current User",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Authorization",
                "value": "Bearer {{token}}",
                "type": "text"
              }
            ],
            "url": {
              "raw": "{{base_url}}/auth/me",
              "host": ["{{base_url}}"],
              "path": ["auth", "me"]
            }
          }
        },
        {
          "name": "Demo Credentials",
          "request": {
            "method": "GET",
            "header": [],
            "url": {
              "raw": "{{base_url}}/auth/demo-credentials",
              "host": ["{{base_url}}"],
              "path": ["auth", "demo-credentials"]
            }
          }
        }
      ]
    },
    {
      "name": "Health Check",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{base_url}}/health",
          "host": ["{{base_url}}"],
          "path": ["health"]
        }
      }
    },
    {
      "name": "CNAB Operations",
      "item": [
        {
          "name": "Upload CNAB File",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Authorization",
                "value": "Bearer {{token}}",
                "type": "text"
              }
            ],
            "body": {
              "mode": "formdata",
              "formdata": [
                {
                  "key": "file",
                  "type": "file",
                  "src": "CNAB.txt"
                }
              ]
            },
            "url": {
              "raw": "{{base_url}}/cnab/upload",
              "host": ["{{base_url}}"],
              "path": ["cnab", "upload"]
            }
          }
        },
        {
          "name": "Get All Transactions",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Authorization",
                "value": "Bearer {{token}}",
                "type": "text"
              }
            ],
            "url": {
              "raw": "{{base_url}}/cnab/transactions",
              "host": ["{{base_url}}"],
              "path": ["cnab", "transactions"]
            }
          }
        },
        {
          "name": "Get Transactions by Store",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Authorization",
                "value": "Bearer {{token}}",
                "type": "text"
              }
            ],
            "url": {
              "raw": "{{base_url}}/cnab/store/BAR DO JOÃO",
              "host": ["{{base_url}}"],
              "path": ["cnab", "store", "BAR DO JOÃO"]
            }
          }
        },
        {
          "name": "Get Store Balances",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Authorization",
                "value": "Bearer {{token}}",
                "type": "text"
              }
            ],
            "url": {
              "raw": "{{base_url}}/cnab/balances",
              "host": ["{{base_url}}"],
              "path": ["cnab", "balances"]
            }
          }
        },
        {
          "name": "Get Statistics",
          "request": {
            "method": "GET",
            "header": [
              {
                "key": "Authorization",
                "value": "Bearer {{token}}",
                "type": "text"
              }
            ],
            "url": {
              "raw": "{{base_url}}/cnab/stats",
              "host": ["{{base_url}}"],
              "path": ["cnab", "stats"]
            }
          }
        }
      ]
    }
  ],
  "variable": [
    {
      "key": "base_url",
      "value": "http://localhost:5099/api",
      "type": "string"
    },
    {
      "key": "token",
      "value": "",
      "type": "string"
    }
  ]
}
```

### Using the Collection

1. **Import the collection** (see JSON above)
2. **Run the "Login" request first** - This will automatically save the JWT token to the collection variable
3. **All other requests** will use the `{{token}}` variable automatically
4. **Token auto-save**: The Login request has a test script that automatically saves the token after successful login

### Environment Variables

The collection includes these variables (automatically managed):

| Variable | Value | Auto-set |
|----------|-------|----------|
| `base_url` | `http://localhost:5099/api` | No |
| `token` | (JWT token) | Yes (after login) |

**Note**: The `token` variable is automatically set when you run the "Login" request. You don't need to copy/paste it manually.

---

## 🔗 Additional Resources

- **Swagger UI**: http://localhost:5099/swagger
- **OpenAPI Spec**: http://localhost:5099/swagger/v1/swagger.json
- **GitHub Repository**: [Your Repo URL]
- **API Status Page**: http://localhost:5099/api/health

---

## 📞 Support

For issues or questions:
- Create an issue on GitHub
- Email: your.email@example.com
- LinkedIn: [Your Profile]

---

<div align="center">
  <p>Built with ❤️ for ByCoders Technical Challenge</p>
</div>
