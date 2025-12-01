# ⚡ Quick Start Guide

Get CNAB Processor running in 3 minutes!

---

## 🎯 Prerequisites

- ✅ Docker installed
- ✅ Docker Compose installed
- ✅ 4GB RAM available

---

## 🚀 3-Step Setup

### Step 1: Clone & Navigate

```bash
git clone https://github.com/yourusername/cnab-processor.git
cd cnab-processor
```

### Step 2: Start Everything

```bash
docker-compose up --build
```

### Step 3: Wait & Access

⏳ **Wait 60 seconds** for SQL Server initialization

Then access:
- 🎨 **Frontend**: http://localhost:3000 (Login page)
- 🔧 **Backend**: http://localhost:5099
- 📚 **Swagger**: http://localhost:5099/swagger

### Step 4: Login

Use these credentials:
- 👤 **Username**: `admin`
- 🔑 **Password**: `Admin@123`

Or just click the "👤 Admin" button on the login page!

---

## ✅ Verify Installation

```bash
# Check if containers are running
docker ps

# You should see:
# - cnab-sqlserver
# - cnab-backend
# - cnab-frontend

# Test backend
curl http://localhost:5099/api/health
# Expected: {"status":"healthy",...}

# Test frontend (open in browser)
start http://localhost:3000  # Windows
open http://localhost:3000   # macOS
xdg-open http://localhost:3000  # Linux
```

---

## 🎮 Try It Out

### 1. Login to the Application

1. Open http://localhost:3000
2. Click "👤 Admin" button (auto-fills credentials)
3. Click "Login"
4. You're in! 🎉

### 2. Upload a Sample File

**Option A: Via UI**
1. Click "📤 Upload" tab
2. Drop a CNAB file or click to browse
3. Watch it process!

**Option B: Create Sample File**

Create `sample.txt`:
```
3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO
5201903010000013200556418150633648****0099153453MARIA SILVA   MERCEARIA 3 IRMÃOS
2201903010000012200845152540736777****1313172712MARCOS PEREIRA LOJA DO Ó - MATRIZ
```

Then upload via UI or get token first:
```bash
# 1. Get token
curl -X POST http://localhost:5099/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'

# 2. Upload (replace YOUR_TOKEN)
curl -X POST http://localhost:5099/api/cnab/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@sample.txt"
```

### 3. View Results

Click "📊 Transactions" tab and see your data!

---

## 🛑 Stop Application

```bash
docker-compose down
```

---

## 🔄 Restart Application

```bash
docker-compose up -d
```

---

## 🧹 Clean Everything

```bash
docker-compose down -v
docker system prune -f
```

---

## 🐛 Troubleshooting

### SQL Server not starting?

```bash
# View logs
docker logs cnab-sqlserver

# Restart
docker-compose restart sqlserver
```

### Backend not responding?

```bash
# View logs
docker logs cnab-backend

# Restart
docker-compose restart backend
```

### Port already in use?

```bash
# Check what's using port 3000/5099
netstat -ano | findstr :3000  # Windows
lsof -i :3000                 # macOS/Linux

# Change port in docker-compose.yml if needed
```

---

## 📚 Next Steps

- Read full [README.md](README.md) for detailed documentation
- Check [API_GUIDE.md](API_GUIDE.md) for API reference
- Run tests: `cd backend && dotnet test`

---

## 🎯 Common Commands

```bash
# View all logs
docker-compose logs -f

# View specific service
docker logs -f cnab-backend

# Rebuild after code changes
docker-compose up -d --build

# Check status
docker-compose ps

# Stop everything
docker-compose down
```

---

<div align="center">
  <p>🚀 Ready to go! Happy coding!</p>
</div>
