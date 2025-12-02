#!/usr/bin/env python3
'''
[OBSOLETE] Sync MAUI Version.Details.xml

⚠️ WARNING: This script is obsolete after refactoring to download MAUI dependencies directly.
   MAUI workload installation (install_latest_maui/install_versioned_maui) now downloads
   Version.Details.xml from upstream on-demand, eliminating the need for local sync.
   
   This script remains for informational/auditing purposes only.

This script downloads MAUI's Version.Details.xml from the specified branch
and merges MAUI workload dependencies into the performance repo's Version.Details.xml.

Usage:
    python scripts/sync_maui_versions.py [--framework net10.0]
    
Examples:
    python scripts/sync_maui_versions.py
    python scripts/sync_maui_versions.py --framework net9.0
    python scripts/sync_maui_versions.py --framework net11.0
'''

import sys
import os
import argparse

# Add src/scenarios to path so we can import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src', 'scenarios'))

from performance.logger import setup_loggers
from shared.mauisharedpython import sync_maui_version_details

def main():
    parser = argparse.ArgumentParser(
        description='Sync MAUI workload dependencies from upstream Version.Details.xml',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )
    parser.add_argument(
        '--framework',
        '-f',
        default='net10.0',
        help='Target framework version (e.g., net10.0, net9.0). Default: net10.0'
    )
    parser.add_argument(
        '--verbose',
        '-v',
        action='store_true',
        help='Enable verbose logging'
    )
    
    args = parser.parse_args()
    
    # Setup logging
    setup_loggers(args.verbose)
    
    print(f"Syncing MAUI Version.Details.xml for {args.framework}...")
    print(f"Source: https://raw.githubusercontent.com/dotnet/maui/{args.framework}/eng/Version.Details.xml")
    print(f"Target: eng/Version.Details.xml")
    print()
    
    try:
        changes_made = sync_maui_version_details(args.framework)
        
        if changes_made:
            print()
            print("✅ Success! MAUI dependencies have been updated.")
            print("   Review the changes in eng/Version.Details.xml")
            print("   Commit the changes if they look correct.")
        else:
            print()
            print("✅ No changes needed - Version.Details.xml is already up-to-date.")
        
        return 0
        
    except Exception as e:
        print()
        print(f"❌ Error: {e}")
        print("   Failed to sync MAUI Version.Details.xml")
        return 1

if __name__ == '__main__':
    sys.exit(main())
