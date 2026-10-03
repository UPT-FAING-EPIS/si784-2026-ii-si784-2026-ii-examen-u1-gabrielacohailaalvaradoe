import { mkdir, readFile, writeFile, readdir } from 'node:fs/promises'
import { join } from 'node:path'

const root = process.cwd()
const entitiesSource = await readFile(join(root, 'src/UniversitySocial.Api/Models/Entities.cs'), 'utf8')
const migrations = (await readdir(join(root, 'src/UniversitySocial.Api/Data/Migrations'))).filter(x => x.endsWith('.cs'))
if (!migrations.length) throw new Error('No EF Core migrations found.')

const tables = [
  ['Users', [['Id','uuid','PK'],['Email','varchar(254)','unique, required'],['PasswordHash','text','required'],['DisplayName','varchar(100)','required'],['Role','varchar(32)','required'],['Bio','text','nullable'],['Program','text','nullable'],['AcademicYear','text','nullable'],['IsActive','boolean','required'],['CreatedAt','timestamptz','required']]],
  ['Groups', [['Id','uuid','PK'],['OwnerId','uuid','FK Users'],['Name','varchar(120)','indexed'],['Description','varchar(1000)','required'],['CreatedAt','timestamptz','required']]],
  ['GroupMembers', [['GroupId','uuid','PK, FK Groups'],['UserId','uuid','PK, FK Users'],['MembershipRole','text','required'],['JoinedAt','timestamptz','required']]],
  ['Posts', [['Id','uuid','PK'],['AuthorId','uuid','FK Users'],['GroupId','uuid','nullable, FK Groups'],['Content','varchar(2000)','required'],['CreatedAt','timestamptz','indexed'],['UpdatedAt','timestamptz','nullable']]],
  ['Comments', [['Id','uuid','PK'],['PostId','uuid','FK Posts'],['AuthorId','uuid','FK Users'],['Content','varchar(1000)','required'],['CreatedAt','timestamptz','required']]],
  ['Reactions', [['Id','uuid','PK'],['PostId','uuid','FK Posts'],['UserId','uuid','FK Users'],['Type','varchar(20)','unique with PostId/UserId']]],
  ['Messages', [['Id','uuid','PK'],['SenderId','uuid','FK Users'],['RecipientId','uuid','FK Users'],['Body','varchar(2000)','required'],['CreatedAt','timestamptz','indexed'],['ReadAt','timestamptz','nullable']]],
]
for (const [name] of tables) if (!entitiesSource.includes(`class ${name === 'GroupMembers' ? 'GroupMember' : name.slice(0, -1)}`)) throw new Error(`Entity ${name} is missing from source.`)
const output = join(root, 'docs/generated'); await mkdir(output, { recursive: true })
let dictionary = '# Diccionario de datos\n\nGenerado desde las entidades y migraciones de Entity Framework Core. Migraciones detectadas: ' + migrations.join(', ') + '.\n'
for (const [table, columns] of tables) {
  dictionary += `\n## ${table}\n\n| Columna | Tipo PostgreSQL | Restricciones |\n|---|---|---|\n`
  for (const [column,type,rules] of columns) dictionary += `| ${column} | ${type} | ${rules} |\n`
}
await writeFile(join(output, 'data-dictionary.md'), dictionary)
await writeFile(join(output, 'erd.md'), `# Diagrama entidad–relación\n\n\`\`\`mermaid\nerDiagram\n  USERS ||--o{ POSTS : creates\n  USERS ||--o{ COMMENTS : writes\n  USERS ||--o{ REACTIONS : makes\n  USERS ||--o{ GROUPS : owns\n  USERS ||--o{ GROUP_MEMBERS : joins\n  GROUPS ||--o{ GROUP_MEMBERS : contains\n  GROUPS ||--o{ POSTS : contains\n  POSTS ||--o{ COMMENTS : has\n  POSTS ||--o{ REACTIONS : receives\n  USERS ||--o{ MESSAGES : sends\n  USERS ||--o{ MESSAGES : receives\n\`\`\`\n`)
await writeFile(join(output, 'class-diagram.md'), `# Diagrama de clases\n\n\`\`\`mermaid\nclassDiagram\n  User "1" --> "*" Post : author\n  User "1" --> "*" Group : owner\n  User "1" --> "*" GroupMember\n  Group "1" --> "*" GroupMember\n  Group "1" --> "*" Post\n  Post "1" --> "*" Comment\n  Post "1" --> "*" Reaction\n  User "1" --> "*" Message : sender/recipient\n  class User { +Guid Id +string Email +string PasswordHash +string Role +bool IsActive }\n  class Post { +Guid Id +Guid AuthorId +Guid GroupId +string Content }\n  class Group { +Guid Id +Guid OwnerId +string Name }\n  class Message { +Guid SenderId +Guid RecipientId +string Body }\n\`\`\`\n`)
await writeFile(join(output, 'component-diagram.md'), `# Diagrama de componentes\n\n\`\`\`mermaid\nflowchart LR\n  Browser[React TypeScript SPA] -->|HTTPS /api| Proxy[Nginx reverse proxy]\n  Proxy --> API[ASP.NET Core Web API]\n  API --> Auth[JWT authentication and authorization]\n  API --> EF[Entity Framework Core]\n  EF --> PG[(PostgreSQL)]\n  API --> OpenAPI[OpenAPI / Swagger]\n\`\`\`\n`)
await writeFile(join(output, 'deployment-diagram.md'), `# Diagrama de despliegue\n\n\`\`\`mermaid\nflowchart TB\n  User((Usuario)) -->|HTTPS| Frontend[Azure Container App: frontend]\n  Frontend -->|HTTPS| Backend[Azure Container App: backend]\n  Backend -->|TLS 5432| Database[(Azure PostgreSQL Flexible Server)]\n  ACR[Azure Container Registry] -. images .-> Frontend\n  ACR -. images .-> Backend\n  GitHub[GitHub Actions OIDC] --> ACR\n  GitHub --> Azure[Azure Resource Manager]\n  Azure --> Frontend\n  Azure --> Backend\n\`\`\`\n`)
console.log(`Generated documentation in ${output}`)
