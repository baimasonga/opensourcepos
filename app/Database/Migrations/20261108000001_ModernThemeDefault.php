<?php

namespace App\Database\Migrations;

use CodeIgniter\Database\Migration;

class ModernThemeDefault extends Migration
{
    public function up(): void
    {
        $this->setTheme(from: 'flatly', to: 'modern');
    }

    public function down(): void
    {
        $this->setTheme(from: 'modern', to: 'flatly');
    }

    /**
     * Only installs still on the stock theme are switched, so a theme an administrator picked is kept.
     */
    private function setTheme(string $from, string $to): void
    {
        $this->db->table('app_config')
            ->where('key', 'theme')
            ->where('value', $from)
            ->update(['value' => $to]);
    }
}
