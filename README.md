# Lalamog - Fighting App

A full-stack fighting app with ASP.NET Core backend and React Native Expo frontend featuring real-time messaging, fight scheduling, and user matching.

## Architecture

### Backend (ASP.NET Core 8.0)
- **Authentication**: JWT tokens with Google OAuth integration
- **Database**: PostgreSQL with PostGIS spatial data support
- **Real-time**: SignalR WebSockets for instant messaging
- **API**: RESTful endpoints for user management, messaging, and fight scheduling

### Frontend (React Native with Expo)
- **UI Framework**: Expo with TypeScript
- **State Management**: React Context API
- **Real-time**: SignalR client for WebSocket connections
- **Navigation**: Expo Router for tab-based navigation

## Features

### ✅ Implemented
- **User Authentication** - Google OAuth with JWT tokens
- **Real-time Messaging** - SignalR WebSocket implementation
- **Fight Scheduling** - Complete booking system with confirmations
- **User Profiles** - Profile management with image uploads
- **Matching System** - Swipe-based user matching
- **Database Migrations** - Entity Framework with PostgreSQL

### 🎯 Core Functionality
- **SessionData Interface** - Comprehensive user session management
- **Message Hub** - Server-side message filtering and distribution
- **Fight Management** - Scheduling, confirmation, and cancellation workflow
- **File Upload** - Profile image handling with secure storage

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+
- PostgreSQL 14+
- Expo CLI

### Backend Setup

1. **Clone and navigate to the project**
   ```bash
   git clone https://github.com/JaspherCad/lalamog-aspnet-reactnative.git
   cd "web api fight"
   ```

2. **Configure authentication** (Required)
   ```bash
   # Edit MyApi/appsettings.json
   {
     "Authentication": {
       "Google": {
         "ClientId": "YOUR_GOOGLE_CLIENT_ID",
         "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
       }
     }
   }
   ```

3. **Setup database connection**
   ```bash
   # Add your PostgreSQL connection string to appsettings.json
   ```

4. **Run migrations and start server**
   ```bash
   cd MyApi
   dotnet ef database update
   dotnet run
   ```

### Frontend Setup

1. **Navigate to frontend and install dependencies**
   ```bash
   cd lalamog
   npm install
   ```

2. **Start Expo development server**
   ```bash
   npx expo start
   ```

## API Endpoints

### Authentication
- `POST /api/OAuth2/google-auth` - Google OAuth authentication
- `GET /api/User/profile` - Get current user profile

### Messaging
- `GET /api/Message/matches` - Get user matches
- `GET /api/Message/conversation/{matchId}` - Get conversation history
- WebSocket: `/messageHub` - Real-time messaging

### Fight Scheduling
- `POST /api/FightingSchedule/schedule` - Schedule a fight
- `POST /api/FightingSchedule/confirm/{scheduleId}` - Confirm fight
- `POST /api/FightingSchedule/cancel/{scheduleId}` - Cancel fight

## WebSocket Integration

### SignalR Connection
```typescript
const connection = new HubConnectionBuilder()
  .withUrl(`${API_BASE_URL}/messageHub`, {
    accessTokenFactory: () => authToken
  })
  .build();

// Join user-specific group
await connection.invoke("JoinGroup", userId);

// Send message
await connection.invoke("SendMessage", matchId, message);
```

### Message Filtering
- Server-side filtering by user groups
- Real-time message distribution
- Connection management with authentication

## Database Schema

### Core Tables
- **AspNetUsers** - User authentication and profiles
- **Messages** - Chat message storage
- **SwipeActions** - User matching actions
- **FightSchedules** - Fight booking and management

### Key Relationships
- Users ↔ Messages (One-to-Many)
- Users ↔ SwipeActions (Many-to-Many)
- Users ↔ FightSchedules (Many-to-Many)

## Environment Variables

### Backend (appsettings.json)
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  },
  "Jwt": {
    "Key": "your-256-bit-secret-key",
    "Issuer": "MyApi",
    "Audience": "MyApi"
  }
}
```

### Frontend (.env)
```bash
EXPO_PUBLIC_API_BASE_URL=https://your-api-url.com
```

## Development

### Backend Development
```bash
cd MyApi
dotnet watch run  # Hot reload during development
```

### Frontend Development
```bash
cd lalamog
npx expo start --clear  # Start with cleared cache
```

### Database Operations
```bash
# Create new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## Security

- **JWT Authentication** - Secure token-based authentication
- **Google OAuth** - OAuth 2.0 integration for user verification
- **CORS Configuration** - Proper cross-origin request handling
- **Input Validation** - DTOs for request/response validation
- **Secret Management** - Environment-based configuration

## Deployment

### Production Checklist
- [ ] Configure production PostgreSQL database
- [ ] Set up environment-specific appsettings.json
- [ ] Configure CORS for production domains
- [ ] Set up SSL certificates
- [ ] Configure SignalR for production scaling

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.

## Support

For questions or issues, please create an issue in the GitHub repository.

---

**Live Demo**: Not yet deployed
**Backend API**: `https://localhost:5248` (Development)
**Frontend**: Expo development server
