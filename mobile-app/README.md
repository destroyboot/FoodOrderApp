# FoodOrderApp Mobile

This is the first mobile scaffold for the customer side of FoodOrderApp.

## Expo SDK

This project is set up for Expo SDK 54 so it can run in Expo Go on a physical device.

Expo's SDK 54 docs reference React Native 0.81, React 19.1.0, and React Native Web 0.21.0.

## Current shape

- Expo + React Native + TypeScript
- Guest ordering session via `X-Guest-Token`
- Auth session via JWT
- Guest order claim on sign-in through `POST /api/account/claim`
- Restaurant selection
- Menu browsing
- Cart basics
- Account entry point with registration / confirmation / sign-in
- Reservation list loading for authenticated users

## Environment

Set the API URL for the environment you are testing in.

If you do not set `EXPO_PUBLIC_API_BASE_URL`, the app now defaults to:

- web: `https://localhost:7234`
- Android emulator: `http://10.0.2.2:5271`
- other native targets: `https://localhost:7234`

You can still override it explicitly when needed.

### Web on the same PC

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="https://localhost:7234"
```

### Android Emulator on Windows

Android emulators cannot reach your host machine through `localhost`. Use:

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="https://10.0.2.2:7234"
```

If the Android emulator rejects the local HTTPS development certificate, use the HTTP profile instead:

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="http://10.0.2.2:5271"
```

### Physical phone on the same Wi-Fi

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="https://YOUR-LAN-IP:7234"
```

If the phone does not trust the local HTTPS certificate, use:

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="http://YOUR-LAN-IP:5271"
```

`https://localhost:7234` only works for web on the same machine. It does not work for physical phones, and it does not work for Android emulators.

## Run

```powershell
cd C:\Users\destr\source\repos\FoodOrderApp\mobile-app
npm install
npm run start
```

Expo SDK 54 requires Node.js 20.19.x or newer.

## Test On PC

You do not need a physical phone for every test pass.

### Option 1: Android Emulator on Windows

This is the closest desktop equivalent to running the real mobile app.

1. Install Android Studio
2. Create and start an Android Virtual Device
3. Run:

```powershell
cd C:\Users\destr\source\repos\FoodOrderApp\mobile-app
npm run start
```

4. In the Expo terminal, press `a`

That opens the app in the Android emulator.

### Option 2: Web Browser

Good for quick UI and flow testing, but not a perfect substitute for native behavior.

```powershell
cd C:\Users\destr\source\repos\FoodOrderApp\mobile-app
$env:EXPO_PUBLIC_API_BASE_URL="https://localhost:7234"
npm run start
```

Then press `w` in the Expo terminal.

### Option 3: iOS Simulator

Only available on macOS. On Windows, the practical desktop path is Android Emulator.

## Important Note About Expo Go On PC

There is no desktop Expo Go app for normal Windows testing. On a PC, use:

- Android Emulator for native-like testing
- Web for quick browser testing

## Important product note

The mobile app is intentionally built around two tracks:

- Guest on-site ordering without account creation
- Signed-in flows for reservations, delivery, and future loyalty/benefit features

That matches the backend cart ownership model already present in the API.

## Push notifications

The app now includes device registration for remote push notifications and uses the backend notifications feed for order updates.

Important limitations while testing:

- web build: no remote push support
- Android Expo Go: remote push requires a development build or production build, not Expo Go
- remote push token registration also requires `extra.eas.projectId` to be set for the Expo project

If remote push is unavailable, the app still refreshes order updates in-app and shows the current push status inside `My Orders`.

## Next steps

- Add QR table deep-link handling
- Add proper navigation stack and detail screens
- Add reservation creation flow
- Add delivery / takeaway checkout branches
- Add customer order history / live order tracking
- Add loyalty / benefits onboarding after guest ordering
