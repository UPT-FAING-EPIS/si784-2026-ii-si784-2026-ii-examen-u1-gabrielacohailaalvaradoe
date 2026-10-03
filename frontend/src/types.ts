export type User = { id: string; email: string; displayName: string; role: string; bio?: string; program?: string; academicYear?: string; isActive: boolean; createdAt: string }
export type Post = { id: string; authorId: string; authorName: string; groupId?: string; content: string; createdAt: string; updatedAt?: string; commentCount: number; reactionCount: number }
export type Group = { id: string; ownerId: string; ownerName: string; name: string; description: string; createdAt: string; memberCount: number; postCount: number; isMember: boolean }
export type Message = { id: string; senderId: string; senderName: string; recipientId: string; recipientName: string; body: string; createdAt: string; readAt?: string }
export type Paged<T> = { items: T[]; page: number; pageSize: number; total: number; totalPages: number }
export type AuthResponse = { token: string; user: User }
