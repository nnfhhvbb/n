﻿using FluentAssertions;
using GitExtUtils.GitUI.Theming;
using GitUI.Theming;
using NSubstitute;

namespace GitUITests.Theming
{
    [TestFixture]
    public class ThemeLoaderTests
    {
        private static readonly IReadOnlyCollection<KnownColor> ThemableSystemColorNames =
            Theme.TestAccessor.SysColorNames;

        private static readonly IReadOnlyCollection<Color> TestColorValues =
            new[]
            {
                Color.Red,
                Color.Green,
            };

        private static readonly IReadOnlyCollection<Color> AlternativeTestColorValues =
            new[]
            {
                Color.LightCoral,
                Color.LightGreen,
            };

        private static readonly IReadOnlyCollection<AppColor> AppColorNames =
            Enum.GetValues(typeof(AppColor))
                .Cast<AppColor>()
                .ToList();

        private const string MockAppThemesDirectory = "c:\\gitextensions\\themes";
        private const string MockUserThemesDirectory = "c:\\appdata\\gitextensions\\themes";

        [Test]
        public void Should_load_any_themable_system_color(
            [ValueSource(nameof(ThemableSystemColorNames))] KnownColor colorName,
            [ValueSource(nameof(TestColorValues))] Color testColorValue)
        {
            IThemeFileReader mockFileReader = CreateMockFileReader(GetThemeContent(colorName, testColorValue));
            IThemeCssUrlResolver mockCssUrlResolver = Substitute.For<IThemeCssUrlResolver>();
            ThemeLoader loader = new(mockCssUrlResolver, mockFileReader);

            Theme theme = LoadTheme(loader);

            theme.GetColor(colorName).ToArgb().Should().Be(testColorValue.ToArgb());
        }

        [Test]
        public void Should_load_any_app_color(
            [ValueSource(nameof(AppColorNames))] AppColor colorName,
            [ValueSource(nameof(TestColorValues))] Color color)
        {
            IThemeFileReader mockFileReader = CreateMockFileReader(GetThemeContent(colorName, color));
            IThemeCssUrlResolver mockCssUrlResolver = Substitute.For<IThemeCssUrlResolver>();
            ThemeLoader loader = new(mockCssUrlResolver, mockFileReader);

            Theme theme = LoadTheme(loader);

            theme.GetColor(colorName).ToArgb().Should().Be(color.ToArgb());
        }

        [Test]
        public void Should_apply_colorblind_overrides_to_system_colors(
            [ValueSource(nameof(ThemableSystemColorNames))] KnownColor colorName)
        {
            Color regularColor = Color.Red;
            Color colorblindColor = Color.Blue;
            IThemeFileReader mockFileReader = CreateMockFileReader(GetThemeContent(colorName, regularColor, colorblindColor));
            IThemeCssUrlResolver mockCssUrlResolver = Substitute.For<IThemeCssUrlResolver>();
            ThemeLoader loader = new(mockCssUrlResolver, mockFileReader);

            Theme regularTheme = LoadTheme(loader);
            regularTheme.GetColor(colorName).ToArgb().Should().Be(regularColor.ToArgb());

            Theme colorblindTheme = LoadTheme(loader, ThemeVariations.Colorblind);

            colorblindTheme.GetColor(colorName).ToArgb().Should().Be(colorblindColor.ToArgb());
        }

        [Test]
        public void Should_apply_colorblind_overrides_to_app_colors(
            [ValueSource(nameof(AppColorNames))] AppColor colorName)
        {
            Color regularColor = Color.Red;
            Color colorblindColor = Color.Blue;
            IThemeFileReader mockFileReader = CreateMockFileReader(GetThemeContent(colorName, regularColor, colorblindColor));
            IThemeCssUrlResolver mockCssUrlResolver = Substitute.For<IThemeCssUrlResolver>();
            ThemeLoader loader = new(mockCssUrlResolver, mockFileReader);

            Theme regularTheme = LoadTheme(loader);
            regularTheme.GetColor(colorName).ToArgb().Should().Be(regularColor.ToArgb());

            Theme colorblindTheme = LoadTheme(loader, ThemeVariations.Colorblind);
            colorblindTheme.GetColor(colorName).ToArgb().Should().Be(colorblindColor.ToArgb());
        }

        [Test]
        public void Should_tolerate_css_comment(
            [Values(KnownColor.Control)] KnownColor colorName,
            [ValueSource(nameof(TestColorValues))] Color testColorValue)
        {
            string commentedContent =
                "/* entire first line with comment */" + Environment.NewLine +
                GetThemeContent(colorName, testColorValue) + "/* comment after definition */";

            IThemeFileReader mockFileReader = CreateMockFileReader(commentedContent);
            IThemeCssUrlResolver mockCssUrlResolver = Substitute.For<IThemeCssUrlResolver>();
            ThemeLoader loader = new(mockCssUrlResolver, mockFileReader);

            Theme theme = LoadTheme(loader);

            theme.GetColor(colorName).ToArgb().Should().Be(testColorValue.ToArgb());
        }

        [Test]
        public void Should_throw_When_unknown_color_name()
        {
            IThemeFileReader mockFileReader = CreateMockFileReader(GetThemeContent("InvalidColorName", Color.Red));
            IThemeCssUrlResolver mockCssUrlResolver = Substitute.For<IThemeCssUrlResolver>();
            ThemeLoader loader = new(mockCssUrlResolver, mockFileReader);

            loader.Invoking(l => LoadTheme(l))
                .Should().Throw<ThemeException>()
                .Which.Message.Should().Contain("InvalidColorName");
        }

        [Ignore("ExCSS version >= 3 do not report parsing errors.")]
        [Test]
        public void Should_throw_When_css_syntax_error()
        {
            IThemeFileReader mockFileReader = CreateMockFileReader(GetThemeContent(KnownColor.Control, Color.Red) + "}");
            IThemeCssUrlResolver mockCssUrlResolver = Substitute.For<IThemeCssUrlResolver>();
            ThemeLoader loader = new(mockCssUrlResolver, mockFileReader);

            loader.Invoking(l => LoadTheme(l))
                .Should().Throw<ThemeException>()
                .Which.Message.Should().Contain("Error parsing CSS");
        }

        [Test]
        public void Should_honor_css_import_directive(
            [Values(KnownColor.Control)] KnownColor colorName,
            [ValueSource(nameof(TestColorValues))] Color baseColor)
        {
            IThemePathProvider pathProvider = CreateMockPathProvider();
            ThemeCssUrlResolver resolver = new(pathProvider);

            string themePath = Path.Combine(pathProvider.AppThemesDirectory, "theme.css");
            string baseThemePath = Path.Combine(pathProvider.AppThemesDirectory, "base.css");

            IThemeFileReader mockFileReader = CreateMockFileReader(new Dictionary<string, string>
            {
                [baseThemePath] = GetThemeContent(colorName, baseColor),
                [themePath] = "@import url(\"base.css\");",
            });

            ThemeLoader loader = new(resolver, mockFileReader);

            Theme theme = loader.LoadTheme(themePath, new ThemeId("theme", isBuiltin: true), allowedClasses: ThemeVariations.None);
            theme.GetColor(colorName).ToArgb().Should().Be(baseColor.ToArgb());
        }

        [Test]
        public void Should_override_imported_color_When_current_theme_redefines_it(
            [Values(KnownColor.Control)] KnownColor colorName,
            [ValueSource(nameof(TestColorValues))] Color baseColor,
            [ValueSource(nameof(AlternativeTestColorValues))] Color colorOverride)
        {
            IThemePathProvider pathProvider = CreateMockPathProvider();
            ThemeCssUrlResolver resolver = new(pathProvider);

            string themePath = Path.Combine(pathProvider.AppThemesDirectory, "theme.css");
            string baseThemePath = Path.Combine(pathProvider.AppThemesDirectory, "base.css");

            IThemeFileReader mockFileReader = CreateMockFileReader(new Dictionary<string, string>
            {
                [baseThemePath] = GetThemeContent(colorName, baseColor),
                [themePath] =
                    "@import url(\"base.css\");" + Environment.NewLine +
                    GetThemeContent(colorName, colorOverride)
            });

            ThemeLoader loader = new(resolver, mockFileReader);

            Theme theme = loader.LoadTheme(themePath, new ThemeId("theme", isBuiltin: true), allowedClasses: ThemeVariations.None);
            theme.GetColor(colorName).ToArgb().Should().Be(colorOverride.ToArgb());
        }

        [Test]
        public void Should_throw_When_cyclic_css_imports()
        {
            IThemePathProvider pathProvider = CreateMockPathProvider();
            ThemeCssUrlResolver resolver = new(pathProvider);

            string themePath = Path.Combine(pathProvider.AppThemesDirectory, "theme.css");
            string baseThemePath = Path.Combine(pathProvider.AppThemesDirectory, "base.css");

            IThemeFileReader mockFileReader = CreateMockFileReader(new Dictionary<string, string>
            {
                [baseThemePath] = "@import url(\"theme.css\");",
                [themePath] = "@import url(\"base.css\");"
            });

            ThemeLoader loader = new(resolver, mockFileReader);

            loader.Invoking(_ => _.LoadTheme(
                    themePath,
                    new ThemeId("theme", isBuiltin: true),
                    allowedClasses: ThemeVariations.None))
                .Should().Throw<ThemeException>()
                .Which.Message.Should().Contain("Cycling CSS import");
        }

        private static IThemePathProvider CreateMockPathProvider()
        {
            IThemePathProvider provider = Substitute.For<IThemePathProvider>();
            provider.ThemeExtension.Returns(".css");
            provider.AppThemesDirectory.Returns(MockAppThemesDirectory);
            provider.UserThemesDirectory.Returns(MockUserThemesDirectory);
            provider.GetThemePath(Arg.Any<ThemeId>())
                .Returns(callInfo =>
                    Path.Combine(
                        callInfo.Arg<ThemeId>().IsBuiltin
                            ? MockAppThemesDirectory
                            : MockUserThemesDirectory,
                        callInfo.Arg<ThemeId>().Name + ".css"));
            return provider;
        }

        private static IThemeFileReader CreateMockFileReader(string content)
        {
            IThemeFileReader reader = Substitute.For<IThemeFileReader>();
            reader.ReadThemeFile(Arg.Any<string>()).Returns(content);
            return reader;
        }

        private static IThemeFileReader CreateMockFileReader(IReadOnlyDictionary<string, string> contentByFile)
        {
            IThemeFileReader reader = Substitute.For<IThemeFileReader>();
            reader.ReadThemeFile(Arg.Any<string>()).Returns(callInfo => contentByFile[callInfo.Arg<string>()]);
            return reader;
        }

        private static string GetThemeContent(object colorName, Color regularColor, Color colorblindColor) =>
            GetThemeContent(
                new Dictionary<object, Color>
                {
                    [colorName] = regularColor,
                    [colorName + ".colorblind"] = colorblindColor
                });

        private static string GetThemeContent(object colorName, Color color) =>
            GetThemeContent(new Dictionary<object, Color> { [colorName] = color });

        private static string GetThemeContent(IEnumerable<KeyValuePair<object, Color>> colorByName) =>
            string.Join(
                Environment.NewLine,
                colorByName.Select(
                    pair => $".{pair.Key} {{ color: {ThemePersistence.TestAccessor.FormatColor(pair.Value)}; }}"));

        private static Theme LoadTheme(ThemeLoader loader, params string[] variations) =>
            loader.LoadTheme(
                "arbitrary\\theme-path.css",
                new ThemeId("arbitrary_theme_name", isBuiltin: true),
                allowedClasses: variations);
    }
}
