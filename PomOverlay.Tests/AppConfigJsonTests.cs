namespace PomOverlay.Tests
{
    public class AppConfigJsonTests
    {
        [Fact]
        public void FromJson_AcceptsEnumNames()
        {
            const string json = """
                {
                  "OverrideMode": "Rest",
                  "Schedules": [ { "Start": "00:00", "End": "06:00", "ApplyMode": "Sleep" } ]
                }
                """;

            var config = AppConfig.FromJson(json)!;

            Assert.Equal(AppConfig.Mode.Rest, config.OverrideMode);
            Assert.Equal(AppConfig.Mode.Sleep, config.Schedules[0].ApplyMode);
        }

        [Fact]
        public void FromJson_AcceptsLegacyNumericEnums()
        {
            // 以前のバージョンが書き出していた形式
            const string json = """
                {
                  "OverrideMode": 0,
                  "Schedules": [ { "Start": "12:00", "End": "13:00", "ApplyMode": 2 } ]
                }
                """;

            var config = AppConfig.FromJson(json)!;

            Assert.Equal(AppConfig.Mode.Auto, config.OverrideMode);
            Assert.Equal(AppConfig.Mode.Rest, config.Schedules[0].ApplyMode);
            // Fps が無い古い config.json は既定値になる
            Assert.Equal(AppConfig.DefaultFps, config.Fps);
        }

        [Fact]
        public void ToJson_WritesEnumNames_AndRoundTrips()
        {
            var config = AppConfig.CreateDefault();
            config.OverrideMode = AppConfig.Mode.Focus;

            string json = config.ToJson();

            Assert.Contains("\"OverrideMode\": \"Focus\"", json);
            Assert.Contains("\"ApplyMode\": \"Sleep\"", json);

            var restored = AppConfig.FromJson(json)!;
            Assert.Equal(AppConfig.Mode.Focus, restored.OverrideMode);
            Assert.Equal(config.Schedules.Count, restored.Schedules.Count);
            Assert.Equal(config.Modes["Rest"].BlurMax, restored.Modes["Rest"].BlurMax);
            Assert.Empty(ConfigValidator.Sanitize(restored));
        }
    }
}
