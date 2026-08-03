import { http } from "@/utils/http";

export type UserResult = {
  success: boolean;
  data: {
    /** 头像 */
    avatar: string;
    /** 用户名 */
    username: string;
    /** 昵称 */
    nickname: string;
    /** 当前登录用户的角色 */
    roles: Array<string>;
    /** 按钮级别权限 */
    permissions: Array<string>;
    /** `token` */
    accessToken: string;
    /** 用于调用刷新`accessToken`的接口时所需的`token` */
    refreshToken: string;
    /** `accessToken` 的过期时间（服务端 ISO 字符串或毫秒时间戳） */
    expires: string | number;
  };
};

export type RbacUser = {
  id: number;
  employeeNo: string;
  name: string;
  role: "super_admin" | "admin" | "editor" | "user";
  enabled: boolean;
};

export type SaveRbacUser = {
  employeeNo: string;
  name: string;
  password?: string;
  role: string;
  enabled: boolean;
};

export type RbacPermissionItem = {
  code: string;
  name: string;
  group: string;
  kind: "menu" | "page" | "api";
  routePath?: string;
  apiPattern?: string;
  methods: string[];
};

export type RbacRolePermissions = {
  role: string;
  permissions: string[];
};

/** 登录 */
export const getLogin = (data?: object) => {
  return http.request<UserResult>("post", "/login", { data });
};

export const getRbacUsers = () =>
  http.request<RbacUser[]>("get", "/api/rbac/users");

export const createRbacUser = (data: SaveRbacUser) =>
  http.request<RbacUser>("post", "/api/rbac/users", { data });

export const updateRbacUser = (id: number, data: SaveRbacUser) =>
  http.request<void>("put", `/api/rbac/users/${id}`, { data });

export const exportRbacUsers = () =>
  http.request<Blob>("get", "/api/rbac/users/export", { responseType: "blob" });

export const importRbacUsers = (data: FormData) =>
  http.post<{ created: number; updated: number; total: number }, FormData>(
    "/api/rbac/users/import",
    { data },
    { headers: { "Content-Type": "multipart/form-data" } }
  );

export const getRbacPermissions = () =>
  http.request<RbacPermissionItem[]>("get", "/api/rbac/permissions");

export const getRbacRolePermissions = () =>
  http.request<RbacRolePermissions[]>("get", "/api/rbac/roles");

export const updateRbacRolePermissions = (role: string, permissions: string[]) =>
  http.request<void>("put", `/api/rbac/roles/${role}/permissions`, {
    data: { permissions }
  });
