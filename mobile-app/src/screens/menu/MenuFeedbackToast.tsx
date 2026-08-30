import { Animated, Text } from "react-native";
import { theme } from "../../lib/theme";

type Props = {
  text: string;
  animation: Animated.Value;
};

export function MenuFeedbackToast({ text, animation }: Props) {
  if (!text) {
    return null;
  }

  return (
    <Animated.View
      pointerEvents="none"
      style={{
        position: "absolute",
        top: 12,
        right: 16,
        zIndex: 20,
        backgroundColor: theme.colors.success,
        borderRadius: theme.radius.medium,
        paddingHorizontal: 12,
        paddingVertical: 8,
        opacity: animation,
        transform: [
          {
            translateY: animation.interpolate({
              inputRange: [0, 1],
              outputRange: [-8, 0],
            }),
          },
        ],
      }}
    >
      <Text style={{ color: "#fff", fontWeight: "700" }}>{text}</Text>
    </Animated.View>
  );
}
