# 🌍 Country Exchange API

A RESTful API that fetches **country data** and **exchange rates** from external APIs, caches them in **MySQL**, and exposes endpoints for querying, refreshing, and viewing statistics — fully Dockerized and reverse-proxied via **Nginx**.

---

## 🚀 Features

✅ Fetches countries from [REST Countries API](https://restcountries.com/v2/all?fields=name,capital,region,population,flag,currencies)
✅ Fetches exchange rates from [Open ER API](https://open.er-api.com/v6/latest/USD)
✅ Computes `estimated_gdp = population × random(1000–2000) ÷ exchange_rate`
✅ Stores and updates data in MySQL
✅ Auto-generates a summary image after each refresh
✅ Full CRUD + filter + sort functionality
✅ Proper error handling (400 / 404 / 503 / 500)
✅ Publicly accessible via Nginx reverse proxy

---

## 🧩 Endpoints

| Method   | Endpoint             | Description                                      |
| -------- | -------------------- | ------------------------------------------------ |
| `POST`   | `/countries/refresh` | Fetch and cache all countries and exchange rates |
| `GET`    | `/countries`         | Get all countries (supports filters and sorting) |
| `GET`    | `/countries/:name`   | Get one country by name                          |
| `DELETE` | `/countries/:name`   | Delete a country record                          |
| `GET`    | `/status`            | Show total countries and last refresh timestamp  |
| `GET`    | `/countries/image`   | Serve generated summary image                    |

---

## ⚙️ Error Handling

| Status | Example JSON Response                                                                                |
| ------ | ---------------------------------------------------------------------------------------------------- |
| 400    | `{ "error": "Validation failed" }`                                                                   |
| 404    | `{ "error": "Country not found" }`                                                                   |
| 503    | `{ "error": "External data source unavailable", "details": "Could not fetch data from [API name]" }` |
| 500    | `{ "error": "Internal server error" }`                                                               |

---

## 🐳 Docker Deployment

### 1️⃣ Create a Docker network

```bash
docker network create countryexchange-net
```

### 2️⃣ Run MySQL container

```bash
docker run -d \
  --name mysql-server \
  --network countryexchange-net \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=country_cache \
  -p 3306:3306 \
  mysql:8
```

### 3️⃣ Run Country Exchange API container

```bash
docker run -d \
  --name countryexchangeapi \
  --network countryexchange-net \
  -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=mysql-server;Port=3306;Database=country_cache;User=root;Password=root;" \
  thed3vjo3l/countryexchangeapi:latest
```

### 4️⃣ Configure Nginx (for public access)

Edit `/etc/nginx/sites-available/default`:

```nginx
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Reload Nginx:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

Your API will now be accessible at:

```
http://<YOUR_PUBLIC_IP>/
```

---

## 💻 Run Locally (Without Docker)

### Prerequisites

* .NET 8 SDK
* MySQL Server running locally
* `dotnet-ef` CLI tool (optional)

### Steps

```bash
git clone https://github.com/<your-username>/CountryExchangeApi.git
cd CountryExchangeApi
dotnet restore
```

Set up your environment variables in `.env` or directly:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=country_cache;User=root;Password=root;"
export ASPNETCORE_URLS="http://localhost:8080"
```

Run the application:

```bash
dotnet run --project CountryExchangeApi
```

Then test:

```bash
curl -X POST http://localhost:8080/countries/refresh
curl http://localhost:8080/countries
curl http://localhost:8080/status
```

---

## 📊 Example Responses

### ✅ Successful Refresh

```json
{
  "message": "Refresh completed",
  "last_refreshed_at": "2025-10-22T18:00:00Z"
}
```

### ❌ External API Down

```json
{
  "error": "External data source unavailable",
  "details": "Could not fetch data from Countries API"
}
```

### ✅ GET /status

```json
{
  "total_countries": 250,
  "last_refreshed_at": "2025-10-22T18:00:00Z"
}
```

---

## 🧐 How It Works

1. `/countries/refresh` fetches data from two external APIs.
2. For each country:

   * Extracts name, region, currency, population.
   * Matches its currency to an exchange rate.
   * Computes `estimated_gdp`.
3. Saves or updates records in MySQL.
4. Generates a summary image at `cache/summary.png` with:

   * Total countries
   * Top 5 by estimated GDP
   * Timestamp of last refresh

---

## 💾 Database Schema

**Table:** `Countries`

| Column            | Type     | Description                |
| ----------------- | -------- | -------------------------- |
| id                | int      | Auto-generated primary key |
| name              | varchar  | Country name (required)    |
| capital           | varchar  | Optional                   |
| region            | varchar  | Optional                   |
| population        | bigint   | Required                   |
| currency_code     | varchar  | Required                   |
| exchange_rate     | decimal  | May be null if unavailable |
| estimated_gdp     | decimal  | Computed                   |
| flag_url          | varchar  | Optional                   |
| last_refreshed_at | datetime | Auto timestamp             |

---

## 🧮 Environment Variables

| Variable                               | Description             | Example                                                                         |
| -------------------------------------- | ----------------------- | ------------------------------------------------------------------------------- |
| `ConnectionStrings__DefaultConnection` | MySQL connection string | `Server=mysql-server;Port=3306;Database=country_cache;User=root;Password=root;` |
| `ASPNETCORE_URLS`                      | API hosting port        | `http://+:8080`                                                                 |

---

## 🧪 Testing (Public or Local)

```bash
curl -X POST http://<PUBLIC_IP>/countries/refresh
curl http://<PUBLIC_IP>/countries?region=Africa
curl http://<PUBLIC_IP>/status
curl http://<PUBLIC_IP>/countries/image
```

---

## 🧮 Tech Stack

* **.NET 8 / C#**
* **Entity Framework Core**
* **MySQL 8**
* **Docker & Nginx**
* **Ubuntu (AWS EC2)**

---

## 👤 Author

**[Your Name]**
Backend Engineer • .NET • Docker • AWS
📧 [[your.email@example.com](mailto:your.email@example.com)]
🌐 [GitHub Profile Link]

---
