import { StatusBar } from "expo-status-bar";
import { SafeAreaView } from "react-native";
import { AppSessionProvider } from "./src/context/AppSessionContext";
import { RootShell } from "./src/RootShell";
import { theme } from "./src/lib/theme";

export default function App() {
  return (
    <AppSessionProvider>
      <SafeAreaView style={{ flex: 1, backgroundColor: theme.colors.background }}>
        <StatusBar style="dark" />
        <RootShell />
      </SafeAreaView>
    </AppSessionProvider>
  );
}
