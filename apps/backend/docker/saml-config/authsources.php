<?php
// saml-config/authsources.php

$config = [
    'admin' => ['core:AdminPassword'],

    'example-userpass' => [
        'exampleauth:UserPass',

        'admin@konfigo.local:admin' => [
            'uid' => ['local-admin'],
            'mail' => ['admin@konfigo.local'],
            'preferred_username' => ['admin'],
            'displayName' => ['admin'],
            'groups' => ['admin', 'developer'],
        ],

        'developer@konfigo.local:developer' => [
            'uid' => ['local-developer'],
            'mail' => ['developer@konfigo.local'],
            'preferred_username' => ['developer'],
            'displayName' => ['developer'],
            'groups' => ['developer'],
        ],
    ],
];