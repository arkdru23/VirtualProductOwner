# 🚀 Przewodnik GitHub Actions CI/CD

## 📊 Twoje Workflows

Masz **2 aktywne workflows**:

### 1️⃣ **.NET CI** (`dotnet-ci.yml`)
**Cel:** Automatyczne budowanie i testowanie

**Wyzwalacze:**
- ✅ Każdy push do dowolnego brancha
- ✅ Każdy Pull Request

**Kroki:**
1. ✅ **Checkout** - Pobiera kod z repo
2. ✅ **Setup .NET 9** - Instaluje .NET 9 SDK
3. ✅ **Restore** - Przywraca NuGet packages
4. ✅ **Build** - Kompiluje projekt (Release)
5. ✅ **Test** - Uruchamia 32 testy + zbiera coverage
6. ✅ **Upload Artifacts** - Zapisuje wyniki testów i coverage

**Czas wykonania:** ~2-3 minuty

---

### 2️⃣ **CodeQL** (`codeql.yml`)
**Cel:** Automatyczne skanowanie bezpieczeństwa kodu

**Wyzwalacze:**
- ✅ Każdy push
- ✅ Każdy Pull Request
- ✅ Raz w tygodniu (schedule)

**Kroki:**
1. ✅ Initialize CodeQL
2. ✅ Build projektu
3. ✅ Analiza bezpieczeństwa
4. ✅ Upload wyników do GitHub Security

---

## 🌐 Jak zobaczyć CI/CD na GitHub:

### **Metoda 1: Actions Tab**

1. **Otwórz repozytorium na GitHub:**
   ```
   https://github.com/arkdru23/VirtualProductOwner
   ```

2. **Kliknij zakładkę "Actions"** (górne menu)

3. **Zobaczysz:**
   - 📊 Lista wszystkich workflow runs
   - ✅ Status (Success/Failed/In Progress)
   - ⏱️ Czas wykonania
   - 🔄 Historia wszystkich uruchomień

### **Metoda 2: Bezpośredni link do Actions**
```
https://github.com/arkdru23/VirtualProductOwner/actions
```

### **Metoda 3: Badge w README**
Dodaj badge do README.md:
```markdown
![.NET CI](https://github.com/arkdru23/VirtualProductOwner/workflows/.NET%20CI/badge.svg)
```

Pokaże: ![.NET CI](https://github.com/arkdru23/VirtualProductOwner/workflows/.NET%20CI/badge.svg)

---

## 📋 Co zobaczysz na stronie Actions:

### **Lista Workflows:**
```
┌─────────────────────────────────────────┐
│ All workflows                           │
├─────────────────────────────────────────┤
│ 🟢 .NET CI                              │
│    Latest: ✅ Success (2m 34s)          │
│    Push to main - 5 minutes ago         │
├─────────────────────────────────────────┤
│ 🔍 CodeQL                               │
│    Latest: ✅ Success (3m 12s)          │
│    Schedule - 2 days ago                │
└─────────────────────────────────────────┘
```

### **Szczegóły pojedynczego run:**
Kliknij na konkretny workflow run aby zobaczyć:

```
.NET CI #42 ✅

Triggered by: push
Branch: main
Commit: f36ead9 "Initial commit"
Duration: 2m 34s

Jobs:
┌────────────────────────────────────────────────┐
│ ✅ build-and-test (2m 34s)                     │
│                                                │
│  ├─ ✅ Checkout (3s)                           │
│  ├─ ✅ Setup .NET 9 (8s)                       │
│  ├─ ✅ Restore (22s)                           │
│  ├─ ✅ Build (45s)                             │
│  ├─ ✅ Test with coverage (52s)                │
│  │   └─ Passed: 32, Failed: 0                 │
│  ├─ ✅ Gather test results (2s)                │
│  ├─ ✅ Upload test results (4s)                │
│  └─ ✅ Upload coverage reports (3s)            │
└────────────────────────────────────────────────┘

Artifacts (2):
  📦 test-results (12.4 KB)
  📦 coverage-reports (45.2 KB)
```

---

## 🔍 Szczegółowa analiza - Krok po kroku:

### **1. Checkout (3s)**
```yaml
- name: Checkout
  uses: actions/checkout@v4
```
📥 Pobiera Twój kod z GitHub

### **2. Setup .NET 9 (8s)**
```yaml
- name: Setup .NET 9
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'
```
⚙️ Instaluje .NET 9 SDK na Ubuntu

### **3. Restore (22s)**
```yaml
- name: Restore
  run: dotnet restore VitrualProductOwner.sln
```
📦 Pobiera wszystkie NuGet packages

### **4. Build (45s)**
```yaml
- name: Build
  run: dotnet build VitrualProductOwner.sln --configuration Release --no-restore
```
🔨 Kompiluje projekt w trybie Release

### **5. Test with coverage (52s)**
```yaml
- name: Test with coverage
  run: dotnet test VitrualProductOwner.sln --configuration Release --no-build --collect:"XPlat Code Coverage"
```
🧪 Uruchamia **32 testy**:
- ✅ Unit tests (StoryGenerator, LlmPromptBuilder, etc.)
- ✅ Integration tests (Auth, CRUD, Conversations)
- 📊 Zbiera code coverage

**Output:**
```
Test run for VirtualProductOwner.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    32, Skipped:     0, Total:    32, Duration: 1 s
```

### **6. Upload Artifacts**
```yaml
- name: Upload test results
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: artifacts/tests

- name: Upload coverage reports
  uses: actions/upload-artifact@v4
  with:
    name: coverage-reports
    path: artifacts/coverage
```
📤 Zapisuje wyniki jako artifacts (możesz je pobrać!)

---

## 💾 Jak pobrać artifacts (wyniki testów):

1. **Przejdź do konkretnego workflow run**
2. **Scroll na dół strony**
3. **Sekcja "Artifacts":**
   ```
   📦 test-results
      └─ Download (12.4 KB)
   
   📦 coverage-reports  
      └─ Download (45.2 KB)
   ```
4. **Kliknij "Download"** aby pobrać ZIP z wynikami

---

## 🔄 Jak przetestować CI/CD:

### **Test 1: Zrób push'a**
```bash
# Zmień coś w kodzie
echo "// test" >> README.md

# Commit i push
git add .
git commit -m "Test CI/CD"
git push

# Automatycznie uruchomi się workflow!
```

### **Test 2: Stwórz Pull Request**
```bash
# Stwórz nowy branch
git checkout -b feature/test-ci

# Zrób zmianę
echo "// test PR" >> README.md

git add .
git commit -m "Test PR CI"
git push -u origin feature/test-ci

# Otwórz PR na GitHub - CI automatycznie się uruchomi!
```

---

## 📊 Status Badge dla README:

Dodaj do `README.md`:

```markdown
# Virtual Product Owner

[![.NET CI](https://github.com/arkdru23/VirtualProductOwner/workflows/.NET%20CI/badge.svg)](https://github.com/arkdru23/VirtualProductOwner/actions)
[![CodeQL](https://github.com/arkdru23/VirtualProductOwner/workflows/CodeQL/badge.svg)](https://github.com/arkdru23/VirtualProductOwner/security/code-scanning)

... reszta README ...
```

**Pokaże:**
- ✅ Badge ze statusem (zielony = success, czerwony = failed)
- 🔗 Klikalne - prowadzi do Actions

---

## 🎯 Najważniejsze linki:

| Co chcesz zobaczyć | Link |
|-------------------|------|
| **Wszystkie Actions** | https://github.com/arkdru23/VirtualProductOwner/actions |
| **.NET CI Workflow** | https://github.com/arkdru23/VirtualProductOwner/actions/workflows/dotnet-ci.yml |
| **CodeQL Workflow** | https://github.com/arkdru23/VirtualProductOwner/actions/workflows/codeql.yml |
| **Ostatnie uruchomienie** | https://github.com/arkdru23/VirtualProductOwner/actions/runs |
| **Security Alerts** | https://github.com/arkdru23/VirtualProductOwner/security |

---

## 🎥 Demo - Co zobaczysz:

### **Po push'u:**
1. GitHub automatycznie wykrywa zmianę
2. Uruchamia `.NET CI` workflow
3. Na stronie Actions pojawi się:
   ```
   🟡 .NET CI #43 (In progress...)
      └─ build-and-test (running...)
   ```
4. Po ~2-3 minutach:
   ```
   ✅ .NET CI #43 (Success)
      └─ build-and-test (2m 34s)
          ├─ 32 tests passed ✅
          └─ Code coverage: 78%
   ```

### **Jeśli test się nie powiedzie:**
```
❌ .NET CI #44 (Failed)
   └─ build-and-test (1m 52s)
       └─ Test with coverage ❌
           Error: Test method StoriesCrudIntegrationTests.CreateStory_Returns201 threw exception:
           System.InvalidOperationException: ...
```

📧 Dostaniesz też email z GitHub!

---

## 🛠️ Monitorowanie w czasie rzeczywistym:

### **Live logs:**
1. Otwórz workflow run (który jest "In progress")
2. Kliknij na job `build-and-test`
3. Zobaczysz **live stream** logów:

```
Run dotnet test VitrualProductOwner.sln
  Determining projects to restore...
  All projects are up-to-date for restore.
  VitrualProductOwner -> /home/runner/work/.../VitrualProductOwner.dll
  VirtualProductOwner.Tests -> /home/runner/work/.../VirtualProductOwner.Tests.dll

Test run for VirtualProductOwner.Tests.dll
Starting test execution, please wait...

[xUnit.net 00:00:00.45]     VirtualProductOwner.Tests.Integration.AuthorizationIntegrationTests.Login_WithValidCredentials_Succeeds [PASS]
[xUnit.net 00:00:00.52]     VirtualProductOwner.Tests.Integration.StoriesCrudIntegrationTests.CreateStory_Returns201 [PASS]
...
```

---

## 📈 Statystyki CI/CD:

Po kilku uruchomieniach zobaczysz:

```
📊 Workflow Usage
┌────────────────────────────────────┐
│ .NET CI                            │
│ ✅ 45 successful runs              │
│ ❌ 2 failed runs                   │
│ 📊 Success rate: 95.7%             │
│ ⏱️  Avg duration: 2m 41s            │
└────────────────────────────────────┘
```

---

## ✅ Podsumowanie - Twój CI/CD robi:

1. ✅ **Automatyczne budowanie** przy każdym push'u
2. ✅ **Uruchamianie 32 testów** (unit + integration)
3. ✅ **Zbieranie code coverage**
4. ✅ **Bezpieczeństwo** - CodeQL scanning
5. ✅ **Artifacts** - wyniki testów do pobrania
6. ✅ **Notyfikacje** - email przy failed builds

**Wszystko działa automatycznie bez Twojej interwencji!** 🚀

---

## 🎯 Następny krok:

**Otwórz w przeglądarce:**
```
https://github.com/arkdru23/VirtualProductOwner/actions
```

I zobacz swój CI/CD w akcji! 🎉
