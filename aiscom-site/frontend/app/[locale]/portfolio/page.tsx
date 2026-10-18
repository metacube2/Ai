import PageHeader from '@/components/visual/PageHeader';
import { Locale, getDictionary } from '@/lib/i18n';
import Card from '@/components/ui/Card';

// Portfolio items will be loaded from CMS/Database
// For now, showing empty state
export default async function PortfolioPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const dictionary = getDictionary(locale as Locale);

  // TODO: Fetch from database
  const portfolioItems: Array<{
    id: number;
    title: string;
    description: string;
    category: string;
    image_path: string;
  }> = [];

  const categories = [
    { id: 'all', label: dictionary.portfolio.categories.all },
    { id: '3dprint', label: dictionary.portfolio.categories['3dprint'] },
    { id: 'software', label: dictionary.portfolio.categories.software },
    { id: 'web', label: dictionary.portfolio.categories.web },
  ];

  return (
    <div className="py-20">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <PageHeader title={dictionary.portfolio.title} subtitle={dictionary.portfolio.subtitle} />

        {/* Category Filter */}
        <div className="flex flex-wrap justify-center gap-3 mb-12">
          {categories.map((category) => (
            <button
              key={category.id}
              className="px-6 py-2 rounded-full text-sm font-medium transition-colors duration-200 bg-primary-light text-gray-300 hover:bg-accent hover:text-primary"
            >
              {category.label}
            </button>
          ))}
        </div>

        {/* Portfolio Grid */}
        {portfolioItems.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {portfolioItems.map((item) => (
              <Card key={item.id} variant="hover" className="overflow-hidden p-0">
                <div className="aspect-video bg-primary-dark relative">
                  {item.image_path ? (
                    <img
                      src={item.image_path}
                      alt={item.title}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center">
                      <svg className="w-16 h-16 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                    </div>
                  )}
                  <span className="absolute top-3 right-3 px-3 py-1 bg-accent text-primary text-xs font-semibold rounded-full">
                    {item.category}
                  </span>
                </div>
                <div className="p-6">
                  <h3 className="text-lg font-semibold text-white mb-2">{item.title}</h3>
                  <p className="text-gray-400 text-sm">{item.description}</p>
                </div>
              </Card>
            ))}
          </div>
        ) : (
          /* Empty State */
          <Card className="text-center py-16">
            <svg className="w-20 h-20 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
            </svg>
            <h3 className="text-xl font-semibold text-white mb-2">{dictionary.portfolio.empty}</h3>
            <p className="text-gray-400">
              {locale === 'de'
                ? 'Schauen Sie bald wieder vorbei!'
                : locale === 'it'
                ? 'Torna presto a trovarci!'
                : 'Check back soon!'}
            </p>
          </Card>
        )}
      </div>
    </div>
  );
}
