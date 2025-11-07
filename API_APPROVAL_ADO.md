# API Quick Reference - Approval & ADO Integration

## 🔄 Approval Workflow API

### Submit Story for Approval
```bash
curl -X POST https://localhost:7223/api/stories/{story-id}/approval/submit \
  -H "Cookie: VPO.Auth=..." \
  -H "X-CSRF-TOKEN: ..."
```

### Approve Story
```bash
curl -X POST https://localhost:7223/api/stories/{story-id}/approval/approve \
  -H "Cookie: VPO.Auth=..." \
  -H "X-CSRF-TOKEN: ..."
```

### Reject Story
```bash
curl -X POST https://localhost:7223/api/stories/{story-id}/approval/reject \
  -H "Cookie: VPO.Auth=..." \
  -H "X-CSRF-TOKEN: ..." \
  -H "Content-Type: application/json" \
  -d '{"reason": "More details needed"}'
```

### Sync to Azure DevOps
```bash
curl -X POST https://localhost:7223/api/stories/{story-id}/approval/sync-to-ado \
  -H "Cookie: VPO.Auth=..." \
  -H "X-CSRF-TOKEN: ..."
```

## 📝 Story Model (Extended)

```json
{
  "id": "guid",
  "title": "string",
  "description": "string",
  "points": 5,
  "acceptanceCriteria": "string",
  "area": "Authentication",
  "state": "New",
  "priority": 2,
  "risk": "Medium",
  "useCase": "Password recovery",
  
  // Approval fields
  "approvalStatus": 0,  // 0=Draft, 1=PendingApproval, 2=Approved, 3=Rejected
  "approvedBy": "user-id",
  "approvedAt": "2024-01-15T10:30:00Z",
  "rejectionReason": "string",
  
  // ADO integration
  "adoWorkItemId": "12345",
  "adoWorkItemUrl": "https://dev.azure.com/...",
  "syncedToAdoAt": "2024-01-15T10:30:00Z"
}
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "AzureDevOps": {
    "Enabled": true,
    "Organization": "your-org",
    "Project": "your-project",
    "PersonalAccessToken": "xxx",
    "WorkItemType": "User Story",
    "DefaultAreaPath": "Project\\Team",
    "DefaultIteration": "Project\\Sprint 1"
  }
}
```

### Environment Variables (Recommended for Production)
```bash
export AzureDevOps__Enabled=true
export AzureDevOps__Organization=your-org
export AzureDevOps__Project=your-project
export AzureDevOps__PersonalAccessToken=your-pat-token
```

## 🎯 Status Flow

```
Draft (0) → Pending Approval (1) → Approved (2) → [Sync to ADO]
                           ↓
                      Rejected (3) → [Edit] → Draft (0)
```

## 🔑 Getting Azure DevOps PAT

1. Go to: https://dev.azure.com/{organization}/_usersSettings/tokens
2. Click "New Token"
3. Name: "VPO Integration"
4. Scopes: `Work Items (Read, Write)`
5. Copy token immediately (shown only once!)

## 📊 Response Examples

### Successful Approval
```json
{
  "status": "approved",
  "approvedBy": "admin",
  "approvedAt": "2024-01-15T10:30:00Z"
}
```

### Successful ADO Sync (Create)
```json
{
  "status": "created",
  "workItemId": "12345",
  "url": "https://dev.azure.com/org/project/_workitems/edit/12345",
  "syncedAt": "2024-01-15T10:30:00Z"
}
```

### Error Response
```json
{
  "error": "Story must be in Pending Approval status to approve"
}
```
