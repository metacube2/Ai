import { NextRequest, NextResponse } from 'next/server';
import { getCmsContent, getAllCmsContent, upsertCmsContent } from '@/lib/db';
import { isAuthenticated } from '@/lib/auth';

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const page = searchParams.get('page');
    const section = searchParams.get('section');

    if (page && section) {
      const content = getCmsContent(page, section);
      return NextResponse.json(content || null);
    }

    // For admin - get all content
    const authenticated = await isAuthenticated();
    if (!authenticated) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const allContent = getAllCmsContent();
    return NextResponse.json(allContent);
  } catch (error) {
    console.error('Get CMS content error:', error);
    return NextResponse.json(
      { error: 'Failed to get content' },
      { status: 500 }
    );
  }
}

export async function POST(request: NextRequest) {
  try {
    // Check authentication
    const authenticated = await isAuthenticated();
    if (!authenticated) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const body = await request.json();
    const { page, section, content_de, content_en, content_it } = body;

    if (!page || !section) {
      return NextResponse.json(
        { error: 'Missing required fields' },
        { status: 400 }
      );
    }

    upsertCmsContent(
      page,
      section,
      content_de || '',
      content_en || content_de || '',
      content_it || content_de || ''
    );

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error('Update CMS content error:', error);
    return NextResponse.json(
      { error: 'Failed to update content' },
      { status: 500 }
    );
  }
}
