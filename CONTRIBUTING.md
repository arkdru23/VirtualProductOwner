# Contributing

Dziękujemy za zainteresowanie projektem. Poniżej szybkie wytyczne, które pomogą sprawnie pracować z repo.

## Wymagania
- .NET SDK 9.0
- (Opcjonalnie) Git z włączonym GitHub Actions dla CI

## Uruchomienie lokalne
1. Przywróć pakiety:
   - `dotnet restore`
2. Zbuduj:
   - `dotnet build`
3. Uruchom:
   - `dotnet run --project VitrualProductOwner`
4. Wejdź na adres HTTPS z konsoli i zaloguj się:
   - Login: `admin`
   - Hasło: `Pass123$`

## Testy
- Wszystkie testy:
  `dotnet test VitrualProductOwner.sln`
- Testy integracyjne używają EF Core InMemory — nie jest potrzebna zewnętrzna baza.

## Formatowanie i jakość
- Przed PR uruchom formatowanie:
  - `dotnet format`
- CI weryfikuje:
  - styl (`dotnet format --verify-no-changes`),
  - build i testy,
  - coverage (próg 70%),
  - CodeQL (analiza bezpieczeństwa).

## Zasady PR
- Małe, samodzielne zmiany (łatwiejszy review).
- Opis PR: co i dlaczego — link do issue (jeśli jest).
- Zielony pipeline CI wymagany.
- Dla zmian większych (architektura, bezpieczeństwo) otwórz dyskusję/issue przed implementacją.

## Uruchamianie e2e (smoke test)
- Wejdź na `/login`, zaloguj się,
- Przejdź do `/generate`, wklej tekst, wygeneruj i zapisz,
- Przejdź do `/stories` — edytuj i usuń pozycję,
- Wyloguj (przycisk Logout — POST).

## Wskazówki
- CSRF: formularze zawierają `<AntiforgeryToken />`, a API oczekuje nagłówka `X-CSRF-TOKEN`.
- Jeśli zmieniasz mechanikę auth, uważaj na kolizje tras (np. POST `/login` używa teraz `/auth/login` jako endpoint API).
