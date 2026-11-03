# LALAMOG - Fighter Matching App

> **⚠️ WORK IN PROGRESS** - This application is currently under active development and not yet complete!

## What is LALAMOG?

**LALAMOG** is like **Tinder, but for SIGAs!** It's a mobile application that connects martial artists, boxers, MMA fighters, and combat sports enthusiasts who want to find sparring partners, schedule matches, and connect with other SIGAs in their area.

### Name Origin
The name **LALAMOG** is inspired by the Filipino delivery app **Lalamove**, but with a humorous twist:
- **"LALA"** - From Lalamove (meaning "move" or "deliver")
- **"LAMOG"** - A Tagalog word meaning **"BEATEN UP"** or **"BATTERED"**

So while Lalamove delivers packages, LALAMOG delivers... well, you get the idea! 😄 HAHA
**Remember: LALAMOG - Where mga siga within the vicinity meet to get LAMOG'd!** 😄
---

## Features

### Current Features (In Development)
- ✅ **User Authentication** - JWT-based secure login and registration
- ✅ **Profile Management** - Create and customize fighter profiles with:
  - Profile pictures
  - Fighting style (Boxing, MMA, Muay Thai, etc.)
  - Experience level
  - Bio and location
  - Availability schedule
- ✅ **Swipe Matching** - Tinder-style card interface for finding fighters
- ✅ **Real-time Chat** - SignalR-powered messaging between matched fighters
- ✅ **Match System** - Mutual like-based matching
- ✅ **Fight Scheduling** - Schedule fights with matched opponents
- ✅ **Location-based Search** - Find fighters near you using geocoding
- 🔄 **Fight Results** - Record and track fight outcomes (In Progress)


<img width="1080" height="849" alt="lalamog" src="https://github.com/user-attachments/assets/84f2f809-ba04-4588-8e5f-bf6c1238cf42" />







### Planned Features
- X Fight statistics and history
- X Rating and review system
- X Public fight events
- X Training partner finder
- X Gym integration
- X Tournament brackets

---

## Tech Stack

### Frontend (Mobile App)
- **React Native** with Expo
- **Expo Router** - File-based navigation
- **TypeScript**
- **Axios** - API communication
- **SignalR** - Real-time messaging
- **React Native Maps** - Location features
- **Expo Location** - Geocoding and GPS

### Backend (Web API)
- **ASP.NET Core 8.0** - REST API
- **.NET Identity** - User authentication
- **JWT** - Token-based auth (15-minute expiration)
- **Entity Framework Core 8.0** - ORM
- **PostgreSQL** - Database
- **PostGIS** - Geographic queries (SRID 4326)
- **SignalR** - WebSocket messaging

### APIs & Services
- **OpenStreetMap Nominatim** - Geocoding (2s timeout)

---

## Project Structure

```
PS C:\Users\Jaspher\Documents\ALL REACT PROJECTS\dotnet\web api fight> tree
.
├── lalamog/                    # React Native mobile app
│   ├── app/                   # Expo Router pages
│   │   ├── (tabs)/           # Tab navigation screens
│   │   │   └── index.tsx     # Swipe matching screen
│   │   ├── Auth.tsx          # Login/Register
│   │   ├── Account.tsx       # Profile editing
│   │   └── settings.tsx      # App settings
│   ├── api/                  # API services
│   │   ├── axiosInstance.ts  # HTTP client
│   │   └── signalRService.ts # WebSocket
│   ├── Components/           # Reusable UI components
│   ├── lib/                  # Context providers & hooks
│   └── types/                # TypeScript definitions
│
└── MyApi/                     # ASP.NET Core Web API
    ├── Controllers/          # API endpoints
    ├── Services/             # Business logic
    ├── Repositories/         # Data access
    ├── Models/               # Database entities
    ├── DTOs/                 # Data transfer objects
    ├── Migrations/           # EF Core migrations
    └── wwwroot/              # Static files (uploads)
```

---

## Getting Started

### Prerequisites
- Node.js (v18+)
- .NET SDK 8.0
- PostgreSQL 14+ with PostGIS extension
- Expo CLI
- iOS Simulator / Android Emulator (or physical device)

### Backend Setup

1. **Configure Database**
   ```bash
   cd MyApi
   # Update connection string in appsettings.Development.json
   ```

2. **Run Migrations**
   ```bash
   dotnet ef database update
   ```

3. **Start API**
   ```bash
   dotnet run
   # API runs on http://localhost:5248
   ```

### Mobile App Setup

1. **Install Dependencies**
   ```bash
   cd lalamog
   npm install
   ```

2. **Configure API URL**
   - Update `baseUrl` in `api/axiosInstance.ts` to your backend URL
   - Update SignalR hub URL in `api/signalRService.ts`

3. **Start Expo**
   ```bash
   npx expo start
   ```

4. **Run on Device**
   - Press `i` for iOS simulator
   - Press `a` for Android emulator
   - Scan QR code with Expo Go app on physical device

---

## Authentication Flow

1. User registers with email, password, full name, and birth date
2. Backend validates and creates user account
3. User logs in and receives JWT token (15-minute expiration)
4. Token is stored in AsyncStorage and added to API requests
5. App checks token validity every 5 minutes
6. User is automatically logged out when token expires

---

## App Screens

### Authentication (`/Auth`)
Toggle between sign-in and sign-up modes with comprehensive form validation.

### Matching (`/`)
Swipe-style card interface showing available fighters. Swipe right to like, left to pass. Mutual likes create matches!

### Chat (`/messages`)
Real-time messaging with matched fighters using SignalR WebSockets.

### Profile (`/Account`)
Edit your fighter profile, upload avatar, set location on map, configure availability.

### Settings (`/settings`)
App preferences and logout functionality.

---

## Key Features Explained

### Swipe Matching Algorithm
- Users are shown available profiles (excluding already swiped and matched users)
- Right swipe = interested, Left swipe = pass
- When both users swipe right on each other → **IT'S A MATCH!**
- Matches use lexicographic user ID sorting to prevent duplicates
- Database trigger automatically creates match records

### Location Features
- **Non-blocking geocoding** - Profiles load instantly, addresses fetch in background
- **2-second timeout** - Prevents indefinite hanging on geocoding requests
- **PostGIS queries** - Geographic distance calculations using SRID 4326
- **Map selection** - Users can pinpoint exact location on interactive map

### Real-time Messaging
- SignalR hub maintains persistent WebSocket connections
- Messages deliver instantly between matched users
- Read receipts and timestamps
- Message history persisted in database

---

## Known Issues / In Progress

- Fight scheduling UI needs refinement
- Match quality indicators not yet implemented
- Fight results tracking incomplete
- No push notifications yet
- Profile filtering preferences need implementation

---

## Recent Improvements

- ✅ Enhanced error handling in authentication
- ✅ JWT token expiration with automatic logout alerts
- ✅ Loading states for better UX
- ✅ Non-blocking geocoding for fast profile loading
- ✅ Fixed avatar display in swipe cards
- ✅ Profile updates now preserve avatar images
- ✅ Removed schedule conflict requirements from matching

---

## Contributing

This is a personal project currently under development. Contributions, suggestions, and feedback are welcome!

---

## meveloper

Built with ❤️ and 🥊 by Jaspher

---

**Remember: LALAMOG - Where fighters meet to get LAMOG'd!** 😄💪
