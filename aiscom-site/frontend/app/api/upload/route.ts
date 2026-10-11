import { NextRequest, NextResponse } from 'next/server';
import { createUpload, getUploads, updateUploadStatus, deleteUpload } from '@/lib/db';
import { isAuthenticated } from '@/lib/auth';
import { v4 as uuidv4 } from 'uuid';
import path from 'path';
import fs from 'fs/promises';

const UPLOAD_DIR = path.join(process.cwd(), 'public', 'uploads');
const MAX_FILE_SIZE = 50 * 1024 * 1024; // 50 MB
const ALLOWED_EXTENSIONS = ['.stl', '.obj'];

export async function POST(request: NextRequest) {
  try {
    const formData = await request.formData();
    const file = formData.get('file') as File;
    const email = formData.get('email') as string;
    const description = formData.get('description') as string;

    if (!file || !email) {
      return NextResponse.json(
        { error: 'Missing required fields' },
        { status: 400 }
      );
    }

    // Validate file extension
    const extension = path.extname(file.name).toLowerCase();
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      return NextResponse.json(
        { error: 'Invalid file type. Only STL and OBJ files are allowed.' },
        { status: 400 }
      );
    }

    // Validate file size
    if (file.size > MAX_FILE_SIZE) {
      return NextResponse.json(
        { error: 'File too large. Maximum size is 50 MB.' },
        { status: 400 }
      );
    }

    // Create upload directory if it doesn't exist
    await fs.mkdir(UPLOAD_DIR, { recursive: true });

    // Generate unique filename
    const uniqueFilename = `${uuidv4()}${extension}`;
    const filePath = path.join(UPLOAD_DIR, uniqueFilename);

    // Save file
    const buffer = Buffer.from(await file.arrayBuffer());
    await fs.writeFile(filePath, buffer);

    // Save to database
    createUpload(uniqueFilename, file.name, file.size, description || '', email);

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error('Upload error:', error);
    return NextResponse.json(
      { error: 'Failed to upload file' },
      { status: 500 }
    );
  }
}

export async function GET() {
  try {
    // Check authentication
    const authenticated = await isAuthenticated();
    if (!authenticated) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const uploads = getUploads();
    return NextResponse.json(uploads);
  } catch (error) {
    console.error('Get uploads error:', error);
    return NextResponse.json(
      { error: 'Failed to get uploads' },
      { status: 500 }
    );
  }
}

export async function PATCH(request: NextRequest) {
  try {
    // Check authentication
    const authenticated = await isAuthenticated();
    if (!authenticated) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const body = await request.json();
    const { id, status } = body;

    if (!id || !status) {
      return NextResponse.json({ error: 'Missing required fields' }, { status: 400 });
    }

    updateUploadStatus(id, status);
    return NextResponse.json({ success: true });
  } catch (error) {
    console.error('Update upload error:', error);
    return NextResponse.json(
      { error: 'Failed to update upload' },
      { status: 500 }
    );
  }
}

export async function DELETE(request: NextRequest) {
  try {
    // Check authentication
    const authenticated = await isAuthenticated();
    if (!authenticated) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const { searchParams } = new URL(request.url);
    const id = searchParams.get('id');
    const filename = searchParams.get('filename');

    if (!id) {
      return NextResponse.json({ error: 'Missing upload ID' }, { status: 400 });
    }

    // Delete file if filename provided
    if (filename) {
      const filePath = path.join(UPLOAD_DIR, filename);
      try {
        await fs.unlink(filePath);
      } catch {
        // File might not exist, that's okay
      }
    }

    deleteUpload(parseInt(id));
    return NextResponse.json({ success: true });
  } catch (error) {
    console.error('Delete upload error:', error);
    return NextResponse.json(
      { error: 'Failed to delete upload' },
      { status: 500 }
    );
  }
}
