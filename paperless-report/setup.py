#!/usr/bin/env python3
"""
Setup-Skript für das Paperless Finance Report Tool.
"""

from setuptools import setup, find_packages
from pathlib import Path

# README einlesen
readme_path = Path(__file__).parent / 'README.md'
long_description = ''
if readme_path.exists():
    long_description = readme_path.read_text(encoding='utf-8')

setup(
    name='paperless-report',
    version='1.0.0',
    description='Finanz-Auswertungstool für Paperless-ngx',
    long_description=long_description,
    long_description_content_type='text/markdown',
    author='Your Name',
    author_email='your.email@example.com',
    url='https://github.com/yourusername/paperless-report',
    license='MIT',

    py_modules=[
        'main',
        'config',
        'paperless_client',
        'extractor',
        'report_generator',
    ],

    include_package_data=True,
    package_data={
        '': ['templates/*.html', 'config.yaml.example'],
    },

    install_requires=[
        'requests>=2.31.0',
        'click>=8.1.7',
        'pyyaml>=6.0.1',
        'jinja2>=3.1.2',
        'python-dateutil>=2.8.2',
        'tabulate>=0.9.0',
        'tqdm>=4.66.1',
    ],

    extras_require={
        'full': [
            'weasyprint>=60.1',
            'diskcache>=5.6.3',
            'colorlog>=6.8.0',
        ],
        'dev': [
            'pytest>=7.4.0',
            'pytest-cov>=4.1.0',
            'black>=23.7.0',
            'flake8>=6.1.0',
            'mypy>=1.5.0',
        ],
    },

    entry_points={
        'console_scripts': [
            'paperless-report=main:main',
        ],
    },

    python_requires='>=3.8',

    classifiers=[
        'Development Status :: 4 - Beta',
        'Environment :: Console',
        'Intended Audience :: End Users/Desktop',
        'License :: OSI Approved :: MIT License',
        'Operating System :: OS Independent',
        'Programming Language :: Python :: 3',
        'Programming Language :: Python :: 3.8',
        'Programming Language :: Python :: 3.9',
        'Programming Language :: Python :: 3.10',
        'Programming Language :: Python :: 3.11',
        'Programming Language :: Python :: 3.12',
        'Topic :: Office/Business :: Financial :: Accounting',
    ],

    keywords='paperless paperless-ngx finance report accounting',
)
