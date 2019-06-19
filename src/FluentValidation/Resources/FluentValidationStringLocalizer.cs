namespace FluentValidation.Resources {
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Globalization;
	using Microsoft.Extensions.Localization;

	internal class FluentValidationStringLocalizer : IStringLocalizer {
		private CultureInfo _culture;
		private readonly ConcurrentDictionary<string, Language> _languages;
		private Language _fallback;

		/// <summary>
		/// Whether localization is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;

		public FluentValidationStringLocalizer() : this(CultureInfo.CurrentUICulture) {
		}

		public FluentValidationStringLocalizer(CultureInfo culture) {
			_culture = culture;
			_fallback = new EnglishLanguage();
			// Initialize with English as the default. Others will be lazily loaded as needed.
			_languages = new ConcurrentDictionary<string, Language>(new[] {
				new KeyValuePair<string, Language>(EnglishLanguage.Culture, _fallback),
			});
		}

		internal FluentValidationStringLocalizer(CultureInfo culture, ConcurrentDictionary<string, Language> languages, Language fallback) {
			_languages = languages;
			_culture = culture;
			_fallback = fallback;
		}

		/// <summary>
		/// Language factory.
		/// </summary>
		/// <param name="culture">The culture code.</param>
		/// <returns>The corresponding Language instance or null.</returns>
		private static Language CreateLanguage(string culture) {
			return culture switch {
				EnglishLanguage.Culture => new EnglishLanguage(),
				AlbanianLanguage.Culture => new AlbanianLanguage(),
				ArabicLanguage.Culture => new ArabicLanguage(),
				ChineseSimplifiedLanguage.Culture => new ChineseSimplifiedLanguage(),
				ChineseTraditionalLanguage.Culture => new ChineseTraditionalLanguage(),
				CroatianLanguage.Culture => new CroatianLanguage(),
				CzechLanguage.Culture => new CzechLanguage(),
				DanishLanguage.Culture => new DanishLanguage(),
				DutchLanguage.Culture => new DutchLanguage(),
				FinnishLanguage.Culture => new FinnishLanguage(),
				FrenchLanguage.Culture => new FrenchLanguage(),
				GermanLanguage.Culture => new GermanLanguage(),
				GeorgianLanguage.Culture => new GeorgianLanguage(),
				GreekLanguage.Culture => new GreekLanguage(),
				HebrewLanguage.Culture => new HebrewLanguage(),
				HindiLanguage.Culture => new HindiLanguage(),
				ItalianLanguage.Culture => new ItalianLanguage(),
				JapaneseLanguage.Culture => new JapaneseLanguage(),
				KoreanLanguage.Culture => new KoreanLanguage(),
				MacedonianLanguage.Culture => new MacedonianLanguage(),
				NorwegianBokmalLanguage.Culture => new NorwegianBokmalLanguage(),
				PersianLanguage.Culture => new PersianLanguage(),
				PolishLanguage.Culture => new PolishLanguage(),
				PortugueseLanguage.Culture => new PortugueseLanguage(),
				PortugueseBrazilLanguage.Culture => new PortugueseBrazilLanguage(),
				RomanianLanguage.Culture => new RomanianLanguage(),
				RussianLanguage.Culture => new RussianLanguage(),
				SlovakLanguage.Culture => new SlovakLanguage(),
				SpanishLanguage.Culture => new SpanishLanguage(),
				SwedishLanguage.Culture => new SwedishLanguage(),
				TurkishLanguage.Culture => new TurkishLanguage(),
				UkrainianLanguage.Culture => new UkrainianLanguage(),
				_=> (Language) null,
				};
		}

		/// <summary>
		/// Removes all languages except the default.
		/// </summary>
		public void Clear() {
			_languages.Clear();
		}

		/// <inheritdoc />
		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) {
			var resourceNames = includeParentCultures
				? GetResourceNamesFromCultureHierarchy(_culture)
				: GetLanguage(_culture)?.GetSupportedKeys();

			if (resourceNames != null) {
				foreach (var name in resourceNames) {
					yield return new LocalizedString(name, GetString(name, _culture));
				}
			}
		}

		/// <inheritdoc />
		[Obsolete("Set CultureInfo.CurrentUICulture instead.")]
		public IStringLocalizer WithCulture(CultureInfo culture) {
			return new FluentValidationStringLocalizer(culture, _languages, _fallback);
		}

		/// <inheritdoc />
		public LocalizedString this[string name] => new LocalizedString(name, GetString(name, _culture));

		/// <inheritdoc />
		public LocalizedString this[string name, params object[] arguments]
			=> new LocalizedString(name, string.Format(GetString(name, _culture), arguments));

		/// <summary>
		/// Gets a string from the specified culture.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		protected virtual string GetString(string name, CultureInfo culture) {
			string value;

			if (Enabled) {
				var languageToUse = GetLanguage(culture) ?? _fallback;
				value = languageToUse.GetTranslation(name);

				// Selected language is missing a translation for this key - fall back to English translation
				// if we're not using english already.
				if (string.IsNullOrEmpty(value) && languageToUse != _fallback) {
					value = _fallback.GetTranslation(name);
				}
			}
			else {
				value = _fallback.GetTranslation(name);
			}

			return value ?? string.Empty;
		}

		private Language GetLanguage(CultureInfo culture) {
			// Find matching translations.
			var languageToUse = _languages.GetOrAdd(culture.Name, CreateLanguage);

			// If we couldn't find translations for this culture, and it's not a neutral culture
			// then try and find translations for the parent culture instead.
			if (languageToUse == null && !culture.IsNeutralCulture) {
				languageToUse = _languages.GetOrAdd(culture.Parent.Name, CreateLanguage);
			}

			return languageToUse;
		}

		private IEnumerable<string> GetResourceNamesFromCultureHierarchy(CultureInfo startingCulture) {
			var currentCulture = startingCulture;
			var resourceNames = new HashSet<string>();

			while (true) {
				var cultureResourceNames = GetLanguage(currentCulture)?.GetSupportedKeys();

				if (cultureResourceNames != null) {
					foreach (var resourceName in cultureResourceNames) {
						resourceNames.Add(resourceName);
					}
				}

				if (currentCulture == currentCulture.Parent) {
					break;
				}

				currentCulture = currentCulture.Parent;
			}

			return resourceNames;
		}
	}
}
