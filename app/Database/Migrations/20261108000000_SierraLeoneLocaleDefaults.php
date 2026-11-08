<?php

namespace App\Database\Migrations;

use CodeIgniter\Database\Migration;

class SierraLeoneLocaleDefaults extends Migration
{
    /**
     * Maps each config key to [stock default, Sierra Leone default]. A key is only
     * switched while it still holds the stock default, so settings an administrator
     * has already customised are never overwritten.
     */
    private const DEFAULTS = [
        'timezone'           => ['America/New_York', 'Africa/Freetown'],
        'number_locale'      => ['en_US', 'en_SL'],
        'currency_symbol'    => ['$', 'Le'],
        'currency_code'      => ['', 'SLE'],
        'country_codes'      => ['us', 'sl'],
        'dateformat'         => ['m/d/Y', 'd/m/Y'],
        'language_code'      => ['en', 'en-GB'],
        'default_tax_1_name' => ['', 'GST'],
        'default_tax_1_rate' => ['', '15'],
    ];

    public function up(): void
    {
        foreach (self::DEFAULTS as $key => [$stockValue, $sierraLeoneValue]) {
            $this->replaceValue($key, $stockValue, $sierraLeoneValue);
        }
    }

    public function down(): void
    {
        foreach (self::DEFAULTS as $key => [$stockValue, $sierraLeoneValue]) {
            $this->replaceValue($key, $sierraLeoneValue, $stockValue);
        }
    }

    private function replaceValue(string $key, string $from, string $to): void
    {
        $builder = $this->db->table('app_config');
        $row     = $builder->where('key', $key)->get()->getRowArray();

        if ($row === null) {
            $builder->insert(['key' => $key, 'value' => $to]);

            return;
        }

        if ($row['value'] === $from) {
            $builder->where('key', $key)->update(['value' => $to]);
        }
    }
}
