[![.NET CI](https://github.com/arkdru23/VirtualProductOwner/workflows/.NET%20CI/badge.svg)](https://github.com/arkdru23/VirtualProductOwner/actions/workflows/dotnet-ci.yml)
[![CodeQL](https://github.com/arkdru23/VirtualProductOwner/workflows/CodeQL/badge.svg)](https://github.com/arkdru23/VirtualProductOwner/actions/workflows/codeql.yml)
[![Tests](https://img.shields.io/badge/tests-32%20passed-brightgreen)](https://github.com/arkdru23/VirtualProductOwner/actions)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)

# Virtual Product Owner (Blazor Server, .NET 9)

Prosta aplikacja Blazor Server do generowania i zarządzania User Stories:
- Uwierzytelnianie cookie (login, logout)
- Stories: CRUD (lista, dodaj, edytuj, usuń)
- Generate: deterministyczny generator user stories z tekstu + **LLM (OpenAI GPT-4o-mini)**
- **Approval Workflow** - Draft → Pending → Approved/Rejected
- **Azure DevOps Integration** - automatyczna synchronizacja Work Items
- Testy jednostkowe i integracyjne (32 testy)
- CI/CD (GitHub Actions) z pokryciem i weryfikacją stylu

## Wymagania
- .NET SDK 9.0
- (Opcjonalnie) GitHub repo z włączonym Actions

## Uruchomienie lokalne
1. Przywróć pakiety:
   - `dotnet restore`
2. Zbuduj:
   - `dotnet build`
3. Uruchom:
   - `dotnet run --project VitrualProductOwner`
4. Wejdź na: adres z konsoli (HTTPS)
5. Zaloguj się (demo):
   - Login: `admin`
   - Hasło: `Pass123$`

## Funkcje
- Auth: logowanie/wylogowanie (cookie, SlidingExpiration, bezpieczne flagi)
- Stories:
  - Lista, dodawanie, edycja, usuwanie (in-memory per użytkownik)
  - Walidacje, spinnery, potwierdzenie usuwania, sortowanie, licznik
- Generate:
  - Wklej tekst (1 linia = 1 story), generuj → podgląd → zapisz wszystko lub pojedyncze
  - Heurystyki punktacji: słowa kluczowe (must/required/critical +2, should/important +1), limit 13
- Testy:
  - Generator (scenariusze i edge cases)
  - StoryService (CRUD, separacja użytkowników)
  - Integracyjne: ochrona stron, antiforgery

## Bezpieczeństwo
- Cookie:
  - HttpOnly, Secure, SameSite=Lax, SlidingExpiration, 60 min
- CSRF:
  - Antiforgery token wymagany dla POST /auth/login i POST /logout
  - Formularze zawierają `<AntiforgeryToken />`
  - Dla wywołań API wymagany nagłówek: `X-CSRF-TOKEN: <token>`
- Nagłówki:
  - X-Content-Type-Options=nosniff
  - X-Frame-Options=DENY
  - Referrer-Policy=no-referrer
  - Permissions-Policy: wyłączone media
  - CSP: self dla skryptów/stylów (style z 'unsafe-inline' dla Bootstrap), brak object-src, brak frame-ancestors
- Rate limiting:
  - /auth/login: 5 żądań/min/IP

## Scenariusz demo (5–7 min)
1. Pokaż zielony pipeline CI (build, test, coverage, format).
2. Wejdź na `/login`, zaloguj się użytkownikiem demo.
3. Otwórz `/generate`, wklej kilka linii tekstu, kliknij Generate, zapisz wszystkie.
4. Otwórz `/stories`: pokaż listę, edytuj jedną pozycję, usuń inną (z potwierdzeniem).
5. Wyloguj się (POST przez przycisk Logout).

## Testy
- Uruchom wszystkie testy:
  - `dotnet test VitrualProductOwner.sln`
- Testy integracyjne używają `WebApplicationFactory<Program>`:
  - Sprawdzają redirect do logowania dla strony chronionej
  - Wymuszają BadRequest dla POST /auth/login bez tokenu antiforgery

## CI (GitHub Actions)
- Plik: `.github/workflows/dotnet-ci.yml`
- Pipeline:
  - dotnet format (verify) – weryfikacja stylu
  - restore, build (Release)
  - test z pokryciem (Cobertura), próg 70% (build fail poniżej)
  - artefakty: test-results (TRX), coverage-reports

## API
- Auth:
  - GET `/antiforgery/login-token` — bez auth; ustawia cookie i zwraca `{ token }` do użycia przy POST logowania
  - POST `/auth/login` — form (Username, Password, `__RequestVerificationToken`); redirect do `/stories` lub `/login?error=1`
  - POST `/logout` — form + `<AntiforgeryToken />`; redirect do `/`
  - GET `/api/antiforgery/token` — auth wymagane; zwraca `{ token }` do wywołań API
- Stories (auth wymagane; nagłówek `X-CSRF-TOKEN: <token>` dla metod modyfikujących):
  - GET `/api/stories/` — lista historii zalogowanego użytkownika
  - POST `/api/stories/` — body JSON `{ title, description, points }` → 201 Created
  - PUT `/api/stories/{id}` — body JSON `{ id, title, description, points }` → 200 OK
  - DELETE `/api/stories/{id}` — 204 No Content

## Health endpoints
- GET `/health` — prosty status usługi (`{ status: "ok" }`)
- GET `/health/ready` — gotowość (sprawdza dostęp do DB); 200 OK lub 503

## Znane ograniczenia
- Brak persystencji – dane trzymane w pamięci procesu (reset po restarcie).
- Hash haseł uproszczony (wariant kursowy).
- CSP dopuszcza inline style (dla Bootstrap). Można zaostrzyć do użycia nonce.

## Checklist akceptacyjna (kurs)
- [x] Auth: logowanie (cookie), ochrona stron; logout przez POST
- [x] CRUD: lista, dodawanie, edycja, usuwanie historii; walidacje, potwierdzenie usunięcia
- [x] Logika: deterministyczny generator i zapis do CRUD
- [x] Test: jednostkowe i integracyjne
- [x] CI: build + test + coverage + format

## Troubleshooting
- Błąd CSRF przy logowaniu/wylogowaniu:
  - Upewnij się, że formularz zawiera `<AntiforgeryToken />` i wysyłasz POST z tej samej domeny/hosta.
- 302 Redirect na `/stories`:
  - Oczekiwane bez zalogowania – przekierowanie do `/login`.
- Test integracyjny nie widzi Program:
  - W pliku Program.cs jest `public partial class Program { }` (wymagane dla WebApplicationFactory).
