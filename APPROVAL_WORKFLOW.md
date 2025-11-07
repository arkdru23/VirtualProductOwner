# Approval Workflow & Azure DevOps Integration

## 🔄 Approval Workflow

### Statusy User Stories:

1. **Draft** (Szkic)
   - Story jest w trakcie tworzenia/edycji
   - Możliwa edycja
   - Nie można zsynchronizować z ADO

2. **Pending Approval** (Oczekuje na zatwierdzenie)
   - Story został wysłany do zatwierdzenia
   - Oczekuje na akcję Approver'a
   - Niemożliwa edycja

3. **Approved** (Zatwierdzony)
   - Story został zatwierdzony
   - Można zsynchronizować z Azure DevOps
   - Zapisane: kto i kiedy zatwierdził

4. **Rejected** (Odrzucony)
   - Story został odrzucony
   - Zapisany powód odrzucenia
   - Można wrócić do Draft i poprawić

### Workflow:

```
Draft → [Submit for Approval] → Pending Approval
                                      ↓
                         [Approve] ← → [Reject]
                            ↓              ↓
                        Approved       Rejected
                            ↓              ↓
                    [Sync to ADO]    [Edit & Resubmit]
```

## 🔗 Azure DevOps Integration

### Konfiguracja:

1. **Uzyskaj Personal Access Token (PAT)**:
   - Zaloguj się do Azure DevOps
   - User Settings → Personal Access Tokens
   - New Token → Wybierz uprawnienia: `Work Items (Read, Write)`
   - Skopiuj token

2. **Skonfiguruj appsettings.json**:

```json
{
  "AzureDevOps": {
    "Enabled": true,
    "Organization": "your-organization",
    "Project": "your-project-name",
    "PersonalAccessToken": "your-pat-token",
    "WorkItemType": "User Story",
    "DefaultAreaPath": "your-project\\Team",
    "DefaultIteration": "your-project\\Sprint 1"
  }
}
```

### Mapowanie pól:

| VPO Field | ADO Field |
|-----------|-----------|
| Title | System.Title |
| Description | System.Description |
| Points | Microsoft.VSTS.Scheduling.StoryPoints |
| AcceptanceCriteria | Microsoft.VSTS.Common.AcceptanceCriteria |
| Area | System.AreaPath |
| Iteration | System.IterationPath |
| Priority | Microsoft.VSTS.Common.Priority |
| Risk | Microsoft.VSTS.Common.Risk |
| State | System.State |

### API Endpoints:

#### 1. Submit for Approval
```http
POST /api/stories/{id}/approval/submit
Authorization: Bearer <token>
X-CSRF-TOKEN: <csrf-token>
```

Response:
```json
{
  "status": "pending_approval"
}
```

#### 2. Approve Story
```http
POST /api/stories/{id}/approval/approve
Authorization: Bearer <token>
X-CSRF-TOKEN: <csrf-token>
```

Response:
```json
{
  "status": "approved",
  "approvedBy": "user-id",
  "approvedAt": "2024-01-15T10:30:00Z"
}
```

#### 3. Reject Story
```http
POST /api/stories/{id}/approval/reject
Authorization: Bearer <token>
X-CSRF-TOKEN: <csrf-token>
Content-Type: application/json

{
  "reason": "Acceptance criteria are not clear enough"
}
```

Response:
```json
{
  "status": "rejected",
  "reason": "Acceptance criteria are not clear enough"
}
```

#### 4. Sync to Azure DevOps
```http
POST /api/stories/{id}/approval/sync-to-ado
Authorization: Bearer <token>
X-CSRF-TOKEN: <csrf-token>
```

Response (new work item):
```json
{
  "status": "created",
  "workItemId": "12345",
  "url": "https://dev.azure.com/org/project/_workitems/edit/12345",
  "syncedAt": "2024-01-15T10:30:00Z"
}
```

Response (updated work item):
```json
{
  "status": "updated",
  "workItemId": "12345",
  "url": "https://dev.azure.com/org/project/_workitems/edit/12345",
  "syncedAt": "2024-01-15T10:35:00Z"
}
```

## 🎯 Typowy workflow:

1. **User tworzy Story** (LLM lub ręcznie)
   - Status: `Draft`

2. **User wysyła do zatwierdzenia**
   - `POST /api/stories/{id}/approval/submit`
   - Status: `Pending Approval`

3. **Approver sprawdza i zatwierdza**
   - `POST /api/stories/{id}/approval/approve`
   - Status: `Approved`

4. **User synchronizuje z ADO**
   - `POST /api/stories/{id}/approval/sync-to-ado`
   - Tworzy Work Item w Azure DevOps
   - Zapisuje `AdoWorkItemId` i `AdoWorkItemUrl`

5. **Późniejsze aktualizacje**
   - Przy ponownej synchronizacji: aktualizuje istniejący Work Item

## 🔐 Security Considerations:

1. **Personal Access Token (PAT)**:
   - Przechowuj w zmiennych środowiskowych lub Azure Key Vault
   - Nadaj minimalne uprawnienia (Read, Write dla Work Items)
   - Regularnie rotuj tokeny

2. **Approver Role** (TODO):
   - Obecnie każdy zalogowany użytkownik może zatwierdzać
   - W przyszłości: dodać role `User` i `Approver`

3. **CSRF Protection**:
   - Wszystkie endpointy wymagają tokenu CSRF

## ⚠️ Error Handling:

| Error | Причина | Решение |
|-------|---------|---------|
| "ADO integration is not enabled" | Brak konfiguracji ADO | Ustaw `Enabled: true` w config |
| "Invalid PAT token" | Błędny lub wygasły token | Wygeneruj nowy PAT token |
| "Project not found" | Błędna nazwa projektu | Sprawdź nazwę w ADO |
| "Story must be approved" | Story nie jest zatwierdzony | Zatwierdź story przed sync |

## 📊 Database Changes:

Nowe pola w tabeli `Stories`:

```sql
-- Approval workflow
ApprovalStatus INT DEFAULT 0,
ApprovedBy VARCHAR(255) NULL,
ApprovedAt DATETIME NULL,
RejectionReason TEXT NULL,

-- ADO integration
AdoWorkItemId VARCHAR(50) NULL,
AdoWorkItemUrl VARCHAR(500) NULL,
SyncedToAdoAt DATETIME NULL
```

## 🚀 Next Steps:

1. **UI Updates** - dodać przyciski i statusy w interfejsie
2. **Roles & Permissions** - implementacja ról User/Approver
3. **Notifications** - email/Slack przy zmianie statusu
4. **Bidirectional Sync** - synchronizacja zmian z ADO do VPO
5. **Bulk Operations** - zatwierdzanie wielu stories naraz
