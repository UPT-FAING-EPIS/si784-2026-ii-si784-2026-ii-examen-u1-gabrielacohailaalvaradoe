# Diccionario de datos

Generado desde las entidades y migraciones de Entity Framework Core. Migraciones detectadas: 20261003013823_InitialCreate.cs, 20261003013823_InitialCreate.Designer.cs, AppDbContextModelSnapshot.cs.

## Users

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| Id | uuid | PK |
| Email | varchar(254) | unique, required |
| PasswordHash | text | required |
| DisplayName | varchar(100) | required |
| Role | varchar(32) | required |
| Bio | text | nullable |
| Program | text | nullable |
| AcademicYear | text | nullable |
| IsActive | boolean | required |
| CreatedAt | timestamptz | required |

## Groups

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| Id | uuid | PK |
| OwnerId | uuid | FK Users |
| Name | varchar(120) | indexed |
| Description | varchar(1000) | required |
| CreatedAt | timestamptz | required |

## GroupMembers

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| GroupId | uuid | PK, FK Groups |
| UserId | uuid | PK, FK Users |
| MembershipRole | text | required |
| JoinedAt | timestamptz | required |

## Posts

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| Id | uuid | PK |
| AuthorId | uuid | FK Users |
| GroupId | uuid | nullable, FK Groups |
| Content | varchar(2000) | required |
| CreatedAt | timestamptz | indexed |
| UpdatedAt | timestamptz | nullable |

## Comments

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| Id | uuid | PK |
| PostId | uuid | FK Posts |
| AuthorId | uuid | FK Users |
| Content | varchar(1000) | required |
| CreatedAt | timestamptz | required |

## Reactions

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| Id | uuid | PK |
| PostId | uuid | FK Posts |
| UserId | uuid | FK Users |
| Type | varchar(20) | unique with PostId/UserId |

## Messages

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| Id | uuid | PK |
| SenderId | uuid | FK Users |
| RecipientId | uuid | FK Users |
| Body | varchar(2000) | required |
| CreatedAt | timestamptz | indexed |
| ReadAt | timestamptz | nullable |
