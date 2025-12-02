# Azure Architecture Diagram

## Expense Management System - Azure Services

This document describes the Azure services deployed by the scripts in this repository.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    Azure Resource Group                                  │
│                                   (rg-expensemgmt-demo)                                 │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────────┐│
│  │                              Core Services (deploy.sh)                              ││
│  │                                                                                     ││
│  │   ┌─────────────────────┐        ┌─────────────────────┐                          ││
│  │   │   User-Assigned     │        │    App Service      │                          ││
│  │   │  Managed Identity   │───────►│   (Windows, S1)     │                          ││
│  │   │ (mid-expensemgmt-*) │        │ (app-expensemgmt-*) │                          ││
│  │   └─────────────────────┘        └──────────┬──────────┘                          ││
│  │            │                                │                                      ││
│  │            │                                │ Managed Identity Auth               ││
│  │            │                                ▼                                      ││
│  │            │                     ┌─────────────────────┐                          ││
│  │            │                     │    Azure SQL        │                          ││
│  │            └────────────────────►│    Database         │                          ││
│  │                                  │ (sql-expensemgmt-*) │                          ││
│  │                                  │  DB: Northwind      │                          ││
│  │                                  │  Azure AD Only Auth │                          ││
│  │                                  └─────────────────────┘                          ││
│  └─────────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────────┐│
│  │                         GenAI Services (deploy-with-chat.sh)                        ││
│  │                                                                                     ││
│  │   ┌─────────────────────┐        ┌─────────────────────┐                          ││
│  │   │  Azure OpenAI       │        │   Azure AI Search   │                          ││
│  │   │ (aoai-expensemgmt-*)│        │(search-expensemgmt*)│                          ││
│  │   │  Location: Sweden   │        │  Location: UK South │                          ││
│  │   │  Model: GPT-4o      │        │  SKU: Basic         │                          ││
│  │   │  SKU: S0            │        │                     │                          ││
│  │   └─────────────────────┘        └─────────────────────┘                          ││
│  │            ▲                                ▲                                      ││
│  │            │                                │                                      ││
│  │            │ Cognitive Services OpenAI User │ Search Index Data Contributor       ││
│  │            │ Role Assignment                │ Role Assignment                      ││
│  │            │                                │                                      ││
│  │            └────────────────┬───────────────┘                                      ││
│  │                             │                                                      ││
│  │                  ┌──────────┴──────────┐                                          ││
│  │                  │   User-Assigned     │                                          ││
│  │                  │  Managed Identity   │                                          ││
│  │                  └─────────────────────┘                                          ││
│  └─────────────────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────────────────┘

                                     ┌─────────────────┐
                                     │                 │
                                     │   End Users     │
                                     │                 │
                                     └────────┬────────┘
                                              │
                                              │ HTTPS
                                              ▼
                              ┌───────────────────────────────┐
                              │        App Service            │
                              │                               │
                              │  ┌─────────────────────────┐ │
                              │  │    ASP.NET Core 8.0     │ │
                              │  │    Razor Pages App      │ │
                              │  │                         │ │
                              │  │  /Index - Expenses List │ │
                              │  │  /AddExpense - New      │ │
                              │  │  /Approve - Manager     │ │
                              │  │  /Chat - AI Assistant   │ │
                              │  │  /swagger - API Docs    │ │
                              │  └─────────────────────────┘ │
                              │                               │
                              │  ┌─────────────────────────┐ │
                              │  │     REST APIs           │ │
                              │  │  /api/Expenses          │ │
                              │  │  /api/Categories        │ │
                              │  │  /api/Statuses          │ │
                              │  │  /api/Users             │ │
                              │  │  /api/Chat              │ │
                              │  └─────────────────────────┘ │
                              └───────────────────────────────┘
```

## Data Flow

1. **User Request** → App Service receives HTTP request
2. **Database Access** → App Service uses Managed Identity to connect to Azure SQL
3. **Chat (Optional)** → Chat requests go through Azure OpenAI with function calling
4. **Response** → Data returned to user via Razor Pages or JSON API

## Authentication

- **SQL Database**: Azure AD-only authentication (no SQL auth)
- **Azure OpenAI**: Managed Identity with "Cognitive Services OpenAI User" role
- **AI Search**: Managed Identity with "Search Index Data Contributor" role
- **App Service**: Uses User-Assigned Managed Identity for all Azure service connections

## Deployment Scripts

| Script | Services Deployed |
|--------|-------------------|
| `deploy.sh` | Resource Group, Managed Identity, App Service, Azure SQL |
| `deploy-with-chat.sh` | All above + Azure OpenAI + AI Search |

## URLs After Deployment

- **Application**: `https://<app-name>.azurewebsites.net/Index`
- **Chat UI**: `https://<app-name>.azurewebsites.net/Chat`
- **API Documentation**: `https://<app-name>.azurewebsites.net/swagger`
