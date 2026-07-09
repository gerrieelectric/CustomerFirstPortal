import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CreateSubmissionRequest {
  submissionType: string;
  submittedByName?: string;
  submittedByEmail?: string;
  submittedByDepartment?: string;
  submittedByEntraObjectId?: string;
  nomineeName: string;
  nomineeEmail: string;
  nomineeDepartment?: string;
  companyName?: string;
  customerName?: string;
  customerEmail?: string;
  recognitionReason: string;
  serviceRating?: number;
}

export interface AttachmentListItem {
  submissionAttachmentId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByName: string;
  uploadedUtc: string;
}

export interface AttachmentDownloadResponse {
  submissionAttachmentId: string;
  originalFileName: string;
  sasUrl: string;
  expiresUtc: string;
}

export interface SubmissionResponse {
  submissionId?: string;
  id?: string;
  submissionType: string;
  submittedByName: string;
  submittedByEmail?: string;
  nomineeEmail?: string;
  nomineeName: string;
  companyName?: string;
  customerName?: string;
  customerEmail?: string;
  recognitionReason: string;
  status: string;
  submittedUtc?: string;
  approvedUtc?: string;
  rejectedUtc?: string;
  rowVersion?: string;
  reviewedByAdminUserId?: number;
  gerrieRecordId?: string;
  createdAt?: string;
  attachments?: AttachmentListItem[];
}

export interface SubmissionListItem {
  submissionId: string;
  submissionType: string;
  status: string;
  submittedByName: string;
  submittedByEmail: string;
  companyName?: string;
  nomineeName: string;
  recognitionReason: string;
  submittedUtc: string;
  reviewedByAdminUserId?: number;
  gerrieRecordId?: string;
}

export interface PaginatedSubmissionsResponse {
  items: SubmissionListItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ListSubmissionsParams {
  pageNumber?: number;
  pageSize?: number;
  status?: string;
  submissionType?: string;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface AdminReviewRequest {
  reviewDecision: 'Approved' | 'Rejected';
  rowVersion: string;
  notes?: string;
  editedRecognitionReason?: string;
}

export interface DirectoryUser {
  entraObjectId: string;
  displayName: string;
  email: string;
  department: string | null;
  profilePhotoUrl?: string;
}

export interface AttachmentUploadResponse {
  submissionAttachmentId: string;
  submissionId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedUtc: string;
}

export interface ExtractedSubmissionFields {
  companyName: string | null;
  customerName: string | null;
  customerEmail: string | null;
  nomineeName: string | null;
  recognitionReason: string | null;
  status: 'Success' | 'Failed' | 'Disabled';
}

export interface EditSuggestion {
  original: string;
  suggested: string;
  category: 'spelling' | 'grammar' | 'name' | 'formatting';
  position: number;
}

export interface SuggestedEditsResponse {
  submissionId: string;
  originalReason: string;
  fullEditedReason: string;
  suggestions: EditSuggestion[];
  status: 'Success' | 'Failed' | 'Stale';
  generatedUtc: string;
  modelName: string;
  promptVersion: number;
}

export interface SuggestionsNotAvailableError {
  message: string;
  code: 'NOT_CONFIGURED' | 'NOT_GENERATED' | 'FAILED';
}

@Injectable({
  providedIn: 'root',
})
export class SubmissionService {
  private apiUrl = `${environment.apiUrl}/submissions`;

  constructor(private http: HttpClient) {}

  createSubmission(
    request: CreateSubmissionRequest,
  ): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(`${this.apiUrl}`, request);
  }

  getSubmission(id: string): Observable<SubmissionResponse> {
    return this.http.get<SubmissionResponse>(`${this.apiUrl}/${id}`);
  }

  listSubmissions(page: number = 1, pageSize: number = 20): Observable<any> {
    return this.http.get(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`);
  }

  listAdminSubmissions(
    params: ListSubmissionsParams = {},
  ): Observable<PaginatedSubmissionsResponse> {
    const searchParams = new URLSearchParams();

    searchParams.set('pageNumber', String(params.pageNumber ?? 1));
    searchParams.set('pageSize', String(params.pageSize ?? 20));

    if (params.status) {
      searchParams.set('status', params.status);
    }

    if (params.submissionType) {
      searchParams.set('submissionType', params.submissionType);
    }

    if (params.searchTerm) {
      searchParams.set('searchTerm', params.searchTerm);
    }

    if (params.sortBy) {
      searchParams.set('sortBy', params.sortBy);
    }

    if (params.sortDescending !== undefined) {
      searchParams.set('sortDescending', String(params.sortDescending));
    }

    return this.http.get<PaginatedSubmissionsResponse>(
      `${this.apiUrl}?${searchParams.toString()}`,
    );
  }

  reviewSubmission(
    submissionId: string,
    request: AdminReviewRequest,
  ): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(
      `${this.apiUrl}/${submissionId}/review`,
      request,
    );
  }

  getDirectoryUsers(): Observable<DirectoryUser[]> {
    return this.http.get<DirectoryUser[]>(`${this.apiUrl}/directory-users`);
  }

  searchDirectoryUsers(query: string): Observable<DirectoryUser[]> {
    const encodedQuery = encodeURIComponent(query);
    return this.http.get<DirectoryUser[]>(
      `${this.apiUrl}/directory-search?query=${encodedQuery}`,
    );
  }

  searchCustomers(query: string): Observable<string[]> {
    const encodedQuery = encodeURIComponent(query);
    return this.http.get<string[]>(
      `${this.apiUrl}/customer-search?query=${encodedQuery}`,
    );
  }

  getUserProfilePhoto(userId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/user-photo/${userId}`, {
      responseType: 'blob',
    });
  }

  uploadAttachment(
    submissionId: string,
    file: File,
  ): Observable<AttachmentUploadResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post<AttachmentUploadResponse>(
      `${this.apiUrl}/${submissionId}/attachments`,
      formData,
    );
  }

  extractSubmissionFields(
    files: File[],
  ): Observable<ExtractedSubmissionFields> {
    const formData = new FormData();
    for (const file of files) {
      formData.append('files', file, file.name);
    }

    return this.http.post<ExtractedSubmissionFields>(
      `${this.apiUrl}/extract-fields`,
      formData,
    );
  }

  getAttachmentDownloadUrl(
    submissionId: string,
    attachmentId: string,
  ): Observable<AttachmentDownloadResponse> {
    return this.http.get<AttachmentDownloadResponse>(
      `${this.apiUrl}/${submissionId}/attachments/${attachmentId}/download`,
    );
  }

  getSuggestedEdits(submissionId: string): Observable<SuggestedEditsResponse> {
    return this.http.get<SuggestedEditsResponse>(
      `${this.apiUrl}/${submissionId}/suggested-edits`,
    );
  }

  regenerateSuggestions(
    submissionId: string,
  ): Observable<SuggestedEditsResponse> {
    return this.http.post<SuggestedEditsResponse>(
      `${this.apiUrl}/${submissionId}/regenerate-suggestions`,
      {},
    );
  }
}
