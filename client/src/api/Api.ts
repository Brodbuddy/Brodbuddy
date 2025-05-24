/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

export enum LoggingLevel {
  Verbose = "Verbose",
  Debug = "Debug",
  Information = "Information",
  Warning = "Warning",
  Error = "Error",
  Fatal = "Fatal",
}

export interface RegisterAnalyzerResponse {
  /** @format guid */
  analyzerId: string;
  name: string;
  nickname?: string | null;
  isNewAnalyzer: boolean;
  isOwner: boolean;
}

export interface RegisterAnalyzerRequest {
  activationCode: string;
  nickname?: string | null;
}

export interface AnalyzerListResponse {
  /** @format guid */
  id: string;
  name: string;
  nickname?: string | null;
  /** @format date-time */
  lastSeen: string | null;
  isOwner: boolean;
}

export interface AdminAnalyzerListResponse {
  /** @format guid */
  id: string;
  name: string;
  macAddress: string;
  firmwareVersion: string | null;
  isActivated: boolean;
  /** @format date-time */
  activatedAt: string | null;
  /** @format date-time */
  lastSeen: string | null;
  /** @format date-time */
  createdAt: string;
}

export interface CreateAnalyzerResponse {
  /** @format guid */
  id: string;
  macAddress: string;
  name: string;
  activationCode: string;
}

export interface CreateAnalyzerRequest {
  macAddress: string;
  name: string;
}

export interface FeatureToggleListResponse {
  features: FeatureToggleResponse[];
}

export interface FeatureToggleResponse {
  /** @format guid */
  id: string;
  name: string;
  description: string | null;
  isEnabled: boolean;
  /** @format int32 */
  rolloutPercentage: number | null;
  /** @format date-time */
  createdAt: string;
  /** @format date-time */
  lastModifiedAt: string | null;
}

export interface FeatureToggleUpdateRequest {
  isEnabled: boolean;
}

export interface FeatureToggleRolloutRequest {
  /** @format int32 */
  percentage: number;
}

export interface LogLevelResponse {
  currentLevel: LoggingLevel;
}

export interface LogLevelUpdateResponse {
  message: string;
  currentLevel: LoggingLevel;
}

export interface LogLevelUpdateRequest {
  logLevel: LoggingLevel;
}

export interface TestTokenResponse {
  accessToken: string;
}

export interface InitiateLoginRequest {
  email: string;
}

export interface LoginVerificationResponse {
  accessToken: string;
}

export interface LoginVerificationRequest {
  email: string;
  /** @format int32 */
  code: number;
}

export interface RefreshTokenResponse {
  accessToken: string;
}

export interface UserInfoResponse {
  email: string;
  isAdmin: boolean;
}

import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  HeadersDefaults,
  ResponseType,
} from "axios";
import axios from "axios";

export type QueryParamsType = Record<string | number, any>;

export interface FullRequestParams
  extends Omit<AxiosRequestConfig, "data" | "params" | "url" | "responseType"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseType;
  /** request body */
  body?: unknown;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown>
  extends Omit<AxiosRequestConfig, "data" | "cancelToken"> {
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<AxiosRequestConfig | void> | AxiosRequestConfig | void;
  secure?: boolean;
  format?: ResponseType;
}

export enum ContentType {
  Json = "application/json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public instance: AxiosInstance;
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private secure?: boolean;
  private format?: ResponseType;

  constructor({
    securityWorker,
    secure,
    format,
    ...axiosConfig
  }: ApiConfig<SecurityDataType> = {}) {
    this.instance = axios.create({
      ...axiosConfig,
      baseURL: axiosConfig.baseURL || "http://localhost:5001",
    });
    this.secure = secure;
    this.format = format;
    this.securityWorker = securityWorker;
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected mergeRequestParams(
    params1: AxiosRequestConfig,
    params2?: AxiosRequestConfig,
  ): AxiosRequestConfig {
    const method = params1.method || (params2 && params2.method);

    return {
      ...this.instance.defaults,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...((method &&
          this.instance.defaults.headers[
            method.toLowerCase() as keyof HeadersDefaults
          ]) ||
          {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected stringifyFormItem(formItem: unknown) {
    if (typeof formItem === "object" && formItem !== null) {
      return JSON.stringify(formItem);
    } else {
      return `${formItem}`;
    }
  }

  protected createFormData(input: Record<string, unknown>): FormData {
    if (input instanceof FormData) {
      return input;
    }
    return Object.keys(input || {}).reduce((formData, key) => {
      const property = input[key];
      const propertyContent: any[] =
        property instanceof Array ? property : [property];

      for (const formItem of propertyContent) {
        const isFileType = formItem instanceof Blob || formItem instanceof File;
        formData.append(
          key,
          isFileType ? formItem : this.stringifyFormItem(formItem),
        );
      }

      return formData;
    }, new FormData());
  }

  public request = async <T = any, _E = any>({
    secure,
    path,
    type,
    query,
    format,
    body,
    ...params
  }: FullRequestParams): Promise<AxiosResponse<T>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const responseFormat = format || this.format || undefined;

    if (
      type === ContentType.FormData &&
      body &&
      body !== null &&
      typeof body === "object"
    ) {
      body = this.createFormData(body as Record<string, unknown>);
    }

    if (
      type === ContentType.Text &&
      body &&
      body !== null &&
      typeof body !== "string"
    ) {
      body = JSON.stringify(body);
    }

    return this.instance.request({
      ...requestParams,
      headers: {
        ...(requestParams.headers || {}),
        ...(type ? { "Content-Type": type } : {}),
      },
      params: query,
      responseType: responseFormat,
      data: body,
      url: path,
    });
  };
}

/**
 * @title Brodbuddy API
 * @version v1
 * @baseUrl http://localhost:5001
 *
 * API til Brodbuddy
 */
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @name Get
   * @request GET:/
   * @secure
   */
  get = (params: RequestParams = {}) =>
    this.request<string, any>({
      path: `/`,
      method: "GET",
      secure: true,
      format: "json",
      ...params,
    });

  analyzer = {
    /**
     * No description
     *
     * @tags Analyzer
     * @name RegisterAnalyzer
     * @request POST:/api/analyzer/register
     * @secure
     */
    registerAnalyzer: (
      data: RegisterAnalyzerRequest,
      params: RequestParams = {},
    ) =>
      this.request<RegisterAnalyzerResponse, any>({
        path: `/api/analyzer/register`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Analyzer
     * @name GetUserAnalyzers
     * @request GET:/api/analyzer
     * @secure
     */
    getUserAnalyzers: (params: RequestParams = {}) =>
      this.request<AnalyzerListResponse[], any>({
        path: `/api/analyzer`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Analyzer
     * @name GetAllAnalyzers
     * @request GET:/api/analyzer/admin/all
     * @secure
     */
    getAllAnalyzers: (params: RequestParams = {}) =>
      this.request<AdminAnalyzerListResponse[], any>({
        path: `/api/analyzer/admin/all`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Analyzer
     * @name CreateAnalyzer
     * @request POST:/api/analyzer/admin/create
     * @secure
     */
    createAnalyzer: (data: CreateAnalyzerRequest, params: RequestParams = {}) =>
      this.request<CreateAnalyzerResponse, any>({
        path: `/api/analyzer/admin/create`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  feature = {
    /**
     * No description
     *
     * @tags Feature
     * @name GetAllFeatures
     * @request GET:/api/feature
     * @secure
     */
    getAllFeatures: (params: RequestParams = {}) =>
      this.request<FeatureToggleListResponse, any>({
        path: `/api/feature`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Feature
     * @name SetFeatureEnabled
     * @request PUT:/api/feature/{featureName}
     * @secure
     */
    setFeatureEnabled: (
      featureName: string,
      data: FeatureToggleUpdateRequest,
      params: RequestParams = {},
    ) =>
      this.request<File, any>({
        path: `/api/feature/${featureName}`,
        method: "PUT",
        body: data,
        secure: true,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Feature
     * @name AddUserToFeature
     * @request POST:/api/feature/{featureName}/users/{userId}
     * @secure
     */
    addUserToFeature: (
      featureName: string,
      userId: string,
      params: RequestParams = {},
    ) =>
      this.request<File, any>({
        path: `/api/feature/${featureName}/users/${userId}`,
        method: "POST",
        secure: true,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Feature
     * @name RemoveUserFromFeature
     * @request DELETE:/api/feature/{featureName}/users/{userId}
     * @secure
     */
    removeUserFromFeature: (
      featureName: string,
      userId: string,
      params: RequestParams = {},
    ) =>
      this.request<File, any>({
        path: `/api/feature/${featureName}/users/${userId}`,
        method: "DELETE",
        secure: true,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Feature
     * @name SetRolloutPercentage
     * @request PUT:/api/feature/{featureName}/rollout
     * @secure
     */
    setRolloutPercentage: (
      featureName: string,
      data: FeatureToggleRolloutRequest,
      params: RequestParams = {},
    ) =>
      this.request<File, any>({
        path: `/api/feature/${featureName}/rollout`,
        method: "PUT",
        body: data,
        secure: true,
        type: ContentType.Json,
        ...params,
      }),
  };
  logging = {
    /**
     * No description
     *
     * @tags Logging
     * @name GetCurrentLogLevel
     * @request GET:/api/logging/level
     * @secure
     */
    getCurrentLogLevel: (params: RequestParams = {}) =>
      this.request<LogLevelResponse, any>({
        path: `/api/logging/level`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Logging
     * @name SetLogLevel
     * @request PUT:/api/logging/level
     * @secure
     */
    setLogLevel: (data: LogLevelUpdateRequest, params: RequestParams = {}) =>
      this.request<LogLevelUpdateResponse, any>({
        path: `/api/logging/level`,
        method: "PUT",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  passwordlessAuth = {
    /**
     * No description
     *
     * @tags PasswordlessAuth
     * @name TestToken
     * @request GET:/api/passwordless-auth/test-token
     * @secure
     */
    testToken: (params: RequestParams = {}) =>
      this.request<TestTokenResponse, any>({
        path: `/api/passwordless-auth/test-token`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags PasswordlessAuth
     * @name InitiateLogin
     * @request POST:/api/passwordless-auth/initiate
     * @secure
     */
    initiateLogin: (data: InitiateLoginRequest, params: RequestParams = {}) =>
      this.request<File, any>({
        path: `/api/passwordless-auth/initiate`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags PasswordlessAuth
     * @name VerifyCode
     * @request POST:/api/passwordless-auth/verify
     * @secure
     */
    verifyCode: (data: LoginVerificationRequest, params: RequestParams = {}) =>
      this.request<LoginVerificationResponse, any>({
        path: `/api/passwordless-auth/verify`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags PasswordlessAuth
     * @name RefreshToken
     * @request POST:/api/passwordless-auth/refresh
     * @secure
     */
    refreshToken: (params: RequestParams = {}) =>
      this.request<RefreshTokenResponse, any>({
        path: `/api/passwordless-auth/refresh`,
        method: "POST",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags PasswordlessAuth
     * @name Logout
     * @request POST:/api/passwordless-auth/logout
     * @secure
     */
    logout: (params: RequestParams = {}) =>
      this.request<File, any>({
        path: `/api/passwordless-auth/logout`,
        method: "POST",
        secure: true,
        ...params,
      }),

    /**
     * No description
     *
     * @tags PasswordlessAuth
     * @name UserInfo
     * @request GET:/api/passwordless-auth/user-info
     * @secure
     */
    userInfo: (params: RequestParams = {}) =>
      this.request<UserInfoResponse, any>({
        path: `/api/passwordless-auth/user-info`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),
  };
}
