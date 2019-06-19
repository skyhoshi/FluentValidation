namespace FluentValidation.Resources {
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Globalization;
	using Microsoft.Extensions.Localization;

	/// <summary>
	/// Class for managing translations of FluentValidation error messages.
	/// </summary>
	public class LanguageManager : IStringLocalizer<LanguageManager> {
		private readonly CultureInfo _cultureOverride;
		private readonly ConcurrentDictionary<string, Language> _languages;
		private readonly Language _fallback;

		/// <summary>
		/// The culture to use.
		/// </summary>
		protected CultureInfo GetCulture() => _cultureOverride ?? CultureInfo.CurrentUICulture;

		/// <summary>
		/// Creates an instance of the LanguageManager. The current culture will be used to translate error messages.
		/// </summary>
		public LanguageManager() {
			_fallback = new EnglishLanguage();
			// Initialize with English as the default. Others will be lazily loaded as needed.
			_languages = new ConcurrentDictionary<string, Language>(new[] {
				new KeyValuePair<string, Language>(EnglishLanguage.Culture, _fallback),
			});
		}

		/// <summary>
		/// Creates a new instance of the language manager.
		/// The specified culture will always be used to translate error messages, regardless of the current culture.
		/// </summary>
		/// <param name="culture">The culture to use for error message translations.</param>
		public LanguageManager(CultureInfo culture) : this() {
			_cultureOverride = culture;
		}

		internal LanguageManager(CultureInfo culture, ConcurrentDictionary<string, Language> languages, Language fallback) {
			_languages = languages;
			_cultureOverride = culture;
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
			var culture = GetCulture();

			var resourceNames = includeParentCultures
				? GetResourceNamesFromCultureHierarchy(culture)
				: GetLanguage(culture)?.GetSupportedKeys();

			if (resourceNames != null) {
				foreach (var name in resourceNames) {
					yield return new LocalizedString(name, GetString(name, culture));
				}
			}
		}

		/// <summary>
		/// Adds additional translations.
		/// </summary>
		/// <param name="language">The culture for which the translation should be added.</param>
		/// <param name="key">The error code/validator name that should be translated</param>
		/// <param name="message">The translated error message</param>
		/// <exception cref="ArgumentNullException"></exception>
		public void AddTranslation(string language, string key, string message) {
			if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
			if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
			if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

			if (!_languages.ContainsKey(language)) {
				_languages[language] = new GenericLanguage(language);
			}

			_languages[language].Translate(key, message);
		}

		/// <inheritdoc />
		[Obsolete("Set CultureInfo.CurrentUICulture instead.")]
		public IStringLocalizer WithCulture(CultureInfo culture) {
			return new LanguageManager(culture, _languages, _fallback);
		}

		/// <inheritdoc />
		public LocalizedString this[string name] => new LocalizedString(name, GetString(name, GetCulture()));

		/// <inheritdoc />
		public LocalizedString this[string name, params object[] arguments]
			=> new LocalizedString(name, string.Format(GetString(name, GetCulture()), arguments));

		/// <summary>
		/// Gets a string from the specified culture.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		protected virtual string GetString(string name, CultureInfo culture) {
			var languageToUse = GetLanguage(culture) ?? _fallback;
			string value = languageToUse.GetTranslation(name);

			// Selected language is missing a translation for this key - fall back to English translation
			// if we're not using english already.
			if (string.IsNullOrEmpty(value) && languageToUse != _fallback) {
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

		// This method is from ASP.NET Core's ResourceManagerStringLocalizer.
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
