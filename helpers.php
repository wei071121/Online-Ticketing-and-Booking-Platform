<?php
// TAR UMT's faculties and centres, used to populate the Faculty dropdown on
// registration and the account page instead of a free-text field.
function tarumt_faculties() {
    return [
        'Faculty of Accountancy, Finance and Business',
        'Faculty of Applied Sciences',
        'Faculty of Computing and Information Technology',
        'Faculty of Built Environment',
        'Faculty of Engineering and Technology',
        'Faculty of Communication and Creative Industries',
        'Faculty of Social Science and Humanities',
        'Centre for Pre-University Studies',
        'Centre for Postgraduate Studies and Research',
        'Centre for Continuing and Professional Education',
        'Centre for Business Incubation and Entrepreneurial Ventures',
        'SME Centre',
        'Student Career Development Centre',
        'Institute of Social Economic Research (ISER)',
    ];
}

// Falls back to a neutral placeholder until an admin uploads a real photo.
//
// An S3-stored photo's image_url is already a full https:// URL - returned
// as-is. A local-disk photo's image_url is root-relative ("/uploads/xxx.jpg")
// and gets turned into a path relative to the current script instead, because
// this app may be hosted as a subdirectory alongside sibling apps (not at the
// web server's document root) - a leading "/uploads/..." would then resolve
// to the wrong app's uploads folder (or nowhere).
function entity_image_url($row) {
    if (!empty($row['image_url'])) {
        if (str_starts_with($row['image_url'], 'https://') || str_starts_with($row['image_url'], 'http://')) {
            return $row['image_url'];
        }

        $relative = ltrim($row['image_url'], '/');
        $prefix = str_contains($_SERVER['SCRIPT_NAME'] ?? '', '/admin/') ? '../' : '';

        $path = __DIR__ . '/' . $relative;
        $version = is_file($path) ? '?v=' . filemtime($path) : '';
        return $prefix . $relative . $version;
    }

    $svg = '<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300">'
         . '<rect width="100%" height="100%" fill="#e6e1ea"/>'
         . '<text x="50%" y="50%" font-size="18" fill="#6b6470" text-anchor="middle" dy=".3em">No photo yet</text>'
         . '</svg>';

    return 'data:image/svg+xml;base64,' . base64_encode($svg);
}

// Curated presentation details for the five featured 2026/2027 events. These
// live alongside the UI rather than in the database so the existing schema and
// order/payment flows remain unchanged. The event ID order matches schema.sql.
function event_presentation($event) {
    $catalog = [
        1 => [
            'event_name' => 'Sunway University Ensemble Presents: Once Upon a Time',
            'event_date' => '2026-10-03',
            'event_time' => '7:30 PM - 9:30 PM',
            'venue' => 'Sunway International School, Iskandar Puteri, Johor',
            'ticket_price' => 35.00,
            'price_label' => 'RM35.00',
            'category' => 'Music & Concerts',
            'image_url' => '/uploads/event-once-upon-a-time.jpg',
            'organizer' => 'Sunway University Ensemble',
            'organizer_copy' => 'A student-led ensemble sharing the joy of orchestral music through memorable live performances.',
            'intro' => 'Step into a world where stories come alive through the power of music.',
            'description' => [
                'Join the Sunway University Ensemble for Once Upon a Time, an enchanting outreach concert celebrating the unforgettable melodies behind beloved stories and cinematic adventures.',
                'From the soaring skies of How to Train Your Dragon and the epic landscapes of The Lord of the Rings to the magical charm of Disney and more worldwide favourites, this is a musical journey through timeless tales that have captured hearts across generations.',
            ],
            'highlights' => ['Live orchestral favourites from beloved films', 'An evening of adventure, nostalgia and wonder', 'A family-friendly concert experience'],
            'registration' => 'Standard admission is RM35, including the stated processing fee. Reserve early to secure your place.',
        ],
        2 => [
            'event_name' => "Lat Tali Lat: A Children's Night Brass Recital",
            'event_date' => '2026-10-10',
            'event_time' => '7:30 PM - 9:00 PM',
            'venue' => 'Recital Hall, Level 4, FASS, Sunway Square, Sunway University',
            'ticket_price' => 35.00,
            'price_label' => 'RM30.00 - RM55.00',
            'category' => 'Music & Concerts',
            'image_url' => '/uploads/event-lat-tali-lat.jpg',
            'organizer' => 'Monday Brass',
            'organizer_copy' => 'A brass chamber formed by young enthusiasts who bring vibrant live music to new audiences.',
            'intro' => 'We can’t bring you back to your childhood, but we can bring childhood back to you!',
            'description' => [
                'Looping your childhood jams? Your Spotify playlist is great, but nothing beats hearing it live with a brass band and a crowd who can jive together.',
                'Bring your friends and family for a playful, uplifting night of familiar tunes, big brass energy and shared childhood memories.',
            ],
            'highlights' => ['Live brass renditions of childhood favourites', 'Group offer: RM175 for 6 people', 'Family combo: RM55 for 1 adult + 1 child'],
            'registration' => 'Tickets start from RM30. Discounts are non-stackable; the standard booking flow reserves the base ticket.',
        ],
        3 => [
            'event_name' => 'Alumni Homecoming Fiesta 2026',
            'event_date' => '2026-10-31',
            'event_time' => '10:00 AM - 3:00 PM',
            'venue' => 'Multipurpose Hall, Level 4, TAR UMT Arena',
            'ticket_price' => 80.00,
            'price_label' => 'Free - RM80.00',
            'category' => 'Community & Social',
            'image_url' => '/uploads/event-alumni-homecoming.jpg',
            'organizer' => 'TAR UMT Department of Alumni Relations',
            'organizer_copy' => 'Reengaging the TARCian community through meaningful moments, lifelong connections and shared pride.',
            'intro' => 'An exquisite tea-inspired celebration where elegance, heritage and enduring TARCian spirit come together.',
            'description' => [
                'Alumni Homecoming Fiesta 2026 is an invitation to Reengage, Reconnect & Rediscover: reunite with familiar faces, rekindle lifelong friendships, forge new connections and relive the moments that shaped us.',
                'Return to your alma mater for meaningful conversations, shared laughter, treasured memories and heartwarming reunions. No matter where life takes us, our hearts will always find their way home.',
            ],
            'highlights' => ['The Story of TARCians movie screening', 'Tea art experience and mini flower-pot workshop', 'Campus ride, kids zone and booth activities'],
            'registration' => 'Registration options range from complimentary access to RM80, depending on the selected programme entitlement.',
        ],
        4 => [
            'event_name' => 'Tax Budget and Me (TBAM) 2027',
            'event_date' => '2026-11-04',
            'event_time' => '9:00 AM - 5:00 PM',
            'venue' => 'Red Brick Theatre, Arena TAR UMT',
            'ticket_price' => 25.00,
            'price_label' => 'RM25.00 - RM50.00',
            'category' => 'Talks & Learning',
            'image_url' => '/uploads/event-tbam-2027.jpg',
            'organizer' => 'TAR UMT Accounting Society',
            'organizer_copy' => 'An academic society focused on creating practical learning and professional connections for future accountants.',
            'intro' => 'Everyone kena TAX: taxation beyond the classroom and closer to everyday life.',
            'description' => [
                'TBAM 2027 is the annual flagship event organised by the TAR UMT Accounting Society, bringing current tax issues, practical learning and knowledge-sharing to an engaging audience of students, alumni and professionals.',
                'Explore Budget 2027 and today’s tax landscape through expert perspectives, Taxopoly and interactive activities designed to make tax knowledge accessible, relevant and engaging.',
            ],
            'highlights' => ['Budget 2027 and current tax insights', 'Taxopoly and interactive learning activities', 'Early-bird registrants receive an exclusive TBAM 2027 T-shirt, while stocks last'],
            'registration' => 'Student and general registration tiers are available from RM25 to RM50. Student registrants must bring a valid student identification card.',
        ],
        5 => [
            'event_name' => 'TARCian Run 2026',
            'event_date' => '2026-11-15',
            'event_time' => '6:30 AM - 11:30 AM',
            'venue' => 'Sports Complex, TAR UMT KL Campus',
            'ticket_price' => 35.00,
            'price_label' => 'RM35.00',
            'category' => 'Sports & Fitness',
            'image_url' => '/uploads/event-tarcian-run.jpg',
            'organizer' => 'TAR UMT Student Sports Committee',
            'organizer_copy' => 'The student team behind one of TAR UMT’s most anticipated annual community fitness events.',
            'intro' => 'A decade-long celebration of fitness, unity and community spirit.',
            'description' => [
                'TARCian Run 2026 proudly marks the 10th edition of TAR UMT’s signature community running event, bringing students, staff, alumni and the public together through an exciting and meaningful shared challenge.',
                'Supported by TAR UMT Alumni, SMJK Chong Hwa Kuala Lumpur and Step8ight, this milestone edition promises a more vibrant atmosphere, exclusive entitlements, engaging activities and strong community participation.',
            ],
            'highlights' => ['7 km community run', 'Exclusive participant entitlements', 'A landmark 10th-edition celebration'],
            'registration' => 'Register for the TARCian Run 2026 7 km session and arrive early for the 6:30 AM event check-in.',
        ],
    ];

    $id = (int)($event['id'] ?? 0);
    if (isset($catalog[$id])) {
        return array_merge($event, $catalog[$id]);
    }

    // Hides the superseded sixth sample event from public event discovery.
    if (($event['event_name'] ?? '') === 'Freshman Welcome Carnival') {
        return null;
    }

    return $event;
}

function event_presentation_list($events) {
    $events = array_values(array_filter(array_map('event_presentation', $events)));
    usort($events, fn($a, $b) => strcmp($a['event_date'], $b['event_date']));
    return $events;
}

// Validates an uploaded photo, then stores it either on S3 (if AWS_S3_BUCKET
// is configured, see config.php) or on local disk (the default). Returns
// [webPath, error] - webPath is either a full S3 https:// URL or a
// root-relative "/uploads/xxx.jpg" path, or null if no file was uploaded or
// it failed.
function handle_image_upload($file, $uploadDir, $prefix = 'photo') {
    if (!isset($file) || $file['error'] === UPLOAD_ERR_NO_FILE) {
        return [null, null];
    }
    if ($file['error'] !== UPLOAD_ERR_OK) {
        return [null, 'Image upload failed. Please try again.'];
    }
    if ($file['size'] > 5 * 1024 * 1024) {
        return [null, 'Image must be smaller than 5MB.'];
    }

    // Check the actual file content, not just the extension/MIME the browser
    // claims, so a renamed .php file can't slip through.
    $imageInfo = getimagesize($file['tmp_name']);
    if ($imageInfo === false) {
        return [null, 'The uploaded file is not a valid image.'];
    }

    $allowedMimes = [
        'image/jpeg' => 'jpg',
        'image/png'  => 'png',
        'image/gif'  => 'gif',
        'image/webp' => 'webp',
    ];
    if (!isset($allowedMimes[$imageInfo['mime']])) {
        return [null, 'Only JPG, PNG, GIF or WEBP images are allowed.'];
    }

    $filename = uniqid($prefix . '_', true) . '.' . $allowedMimes[$imageInfo['mime']];

    if (AWS_S3_BUCKET !== '') {
        return s3_put_object($filename, file_get_contents($file['tmp_name']), $imageInfo['mime']);
    }

    if (!is_dir($uploadDir)) {
        mkdir($uploadDir, 0755, true);
    }
    if (!move_uploaded_file($file['tmp_name'], $uploadDir . '/' . $filename)) {
        return [null, 'Could not save the uploaded image.'];
    }

    return ['/uploads/' . $filename, null];
}

// Deletes a previously uploaded image, from S3 or local disk depending on
// which one image_url points at.
function delete_image_file($imageUrl, $uploadDir) {
    if (!$imageUrl) {
        return;
    }
    if (str_starts_with($imageUrl, 'https://') || str_starts_with($imageUrl, 'http://')) {
        s3_delete_object($imageUrl);
        return;
    }
    if (str_starts_with($imageUrl, '/uploads/')) {
        $path = $uploadDir . '/' . basename($imageUrl);
        if (is_file($path)) {
            unlink($path);
        }
    }
}

// ============================================================================
// S3 upload support (Signature Version 4, no AWS SDK/Composer dependency).
// Only used when AWS_S3_BUCKET is set in config.php - local disk is the
// default and needs none of this. The signing logic here is verified
// byte-for-byte against AWS's own published SigV4 test suite.
// ============================================================================

// Builds the canonical request + the list of header names that were signed,
// per the SigV4 spec: https://docs.aws.amazon.com/general/latest/gr/sigv4-create-canonical-request.html
function s3_canonical_request($method, $path, $headers, $payloadHash) {
    $sorted = $headers;
    ksort($sorted);
    $canonicalHeaders = '';
    foreach ($sorted as $name => $value) {
        $canonicalHeaders .= strtolower($name) . ':' . trim($value) . "\n";
    }
    $signedHeaders = implode(';', array_map('strtolower', array_keys($sorted)));
    $canonicalRequest = implode("\n", [$method, $path, '', $canonicalHeaders, $signedHeaders, $payloadHash]);
    return [$canonicalRequest, $signedHeaders];
}

// Signs an S3 request and returns [host, headers] with the Authorization
// header already filled in.
function s3_sign($method, $bucket, $region, $key, $payload, $credentials) {
    $host = "$bucket.s3.$region.amazonaws.com";
    $amzDate = gmdate('Ymd\THis\Z');
    $dateStamp = gmdate('Ymd');
    $payloadHash = hash('sha256', $payload);

    $headers = [
        'Host' => $host,
        'X-Amz-Date' => $amzDate,
        'X-Amz-Content-Sha256' => $payloadHash,
    ];
    if (!empty($credentials['token'])) {
        $headers['X-Amz-Security-Token'] = $credentials['token'];
    }

    [$canonicalRequest, $signedHeaders] = s3_canonical_request($method, '/' . $key, $headers, $payloadHash);

    $service = 's3';
    $credentialScope = "$dateStamp/$region/$service/aws4_request";
    $stringToSign = implode("\n", [
        'AWS4-HMAC-SHA256',
        $amzDate,
        $credentialScope,
        hash('sha256', $canonicalRequest),
    ]);

    $kDate = hash_hmac('sha256', $dateStamp, 'AWS4' . $credentials['secret_key'], true);
    $kRegion = hash_hmac('sha256', $region, $kDate, true);
    $kService = hash_hmac('sha256', $service, $kRegion, true);
    $signingKey = hash_hmac('sha256', 'aws4_request', $kService, true);
    $signature = hash_hmac('sha256', $stringToSign, $signingKey);

    $headers['Authorization'] = "AWS4-HMAC-SHA256 Credential={$credentials['access_key']}/$credentialScope, "
        . "SignedHeaders=$signedHeaders, Signature=$signature";

    return [$host, $headers];
}

// Gets S3 credentials one of two ways: first by asking the EC2 instance's
// own metadata service (IMDSv2) for whatever IAM role is attached - the
// preferred way, since those credentials are temporary and rotated
// automatically with nothing to leak. If there's no role to ask (e.g.
// running locally, or an AWS Academy Learner Lab where you can't attach
// one), falls back to explicit AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY/
// AWS_SESSION_TOKEN from config.php (set as environment variables, e.g.
// copied from a Learner Lab's "AWS Details" panel - never hardcoded/
// committed). Returns null if neither is available, quickly (short
// timeouts on the metadata service calls) so this never hangs a request.
function s3_instance_credentials() {
    $credentials = s3_role_credentials();
    if ($credentials) {
        return $credentials;
    }

    if (AWS_ACCESS_KEY_ID !== '' && AWS_SECRET_ACCESS_KEY !== '') {
        return [
            'access_key' => AWS_ACCESS_KEY_ID,
            'secret_key' => AWS_SECRET_ACCESS_KEY,
            'token' => AWS_SESSION_TOKEN,
        ];
    }

    return null;
}

// The IMDSv2 half of s3_instance_credentials() - split out so the fallback
// logic above stays easy to follow.
function s3_role_credentials() {
    $tokenCtx = stream_context_create(['http' => [
        'method' => 'PUT',
        'header' => "X-aws-ec2-metadata-token-ttl-seconds: 21600\r\n",
        'timeout' => 1,
        'ignore_errors' => true,
    ]]);
    $token = @file_get_contents('http://169.254.169.254/latest/api/token', false, $tokenCtx);
    if ($token === false || $token === '') {
        return null;
    }

    $metaCtx = stream_context_create(['http' => [
        'method' => 'GET',
        'header' => "X-aws-ec2-metadata-token: $token\r\n",
        'timeout' => 1,
        'ignore_errors' => true,
    ]]);
    $roleName = trim((string)@file_get_contents(
        'http://169.254.169.254/latest/meta-data/iam/security-credentials/',
        false,
        $metaCtx
    ));
    if ($roleName === '') {
        return null;
    }

    $credsJson = @file_get_contents(
        "http://169.254.169.254/latest/meta-data/iam/security-credentials/$roleName",
        false,
        $metaCtx
    );
    $creds = $credsJson ? json_decode($credsJson, true) : null;
    if (!isset($creds['AccessKeyId'], $creds['SecretAccessKey'], $creds['Token'])) {
        return null;
    }

    return [
        'access_key' => $creds['AccessKeyId'],
        'secret_key' => $creds['SecretAccessKey'],
        'token' => $creds['Token'],
    ];
}

// Uploads $data to S3 under $key. Returns [publicUrl, error], matching the
// shape handle_image_upload()'s callers already expect.
function s3_put_object($key, $data, $contentType) {
    $credentials = s3_instance_credentials();
    if (!$credentials) {
        return [null, 'Could not get S3 credentials: no IAM role is attached to this instance, and '
            . 'AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY are not set either. See config.php.'];
    }

    [$host, $headers] = s3_sign('PUT', AWS_S3_BUCKET, AWS_S3_REGION, $key, $data, $credentials);
    $headers['Content-Type'] = $contentType;

    $headerLines = '';
    foreach ($headers as $name => $value) {
        $headerLines .= "$name: $value\r\n";
    }

    $context = stream_context_create(['http' => [
        'method' => 'PUT',
        'header' => $headerLines,
        'content' => $data,
        'timeout' => 20,
        'ignore_errors' => true,
    ]]);

    @file_get_contents("https://$host/$key", false, $context);
    $status = s3_response_status($http_response_header ?? []);

    if ($status !== 200) {
        return [null, "S3 upload failed (HTTP $status)."];
    }

    return ["https://$host/$key", null];
}

// Deletes an object previously uploaded to S3, given the URL stored in
// image_url. Does nothing if the URL doesn't belong to the configured
// bucket (defensive - shouldn't happen in practice).
function s3_delete_object($url) {
    $host = AWS_S3_BUCKET . '.s3.' . AWS_S3_REGION . '.amazonaws.com';
    $prefix = "https://$host/";
    if (!str_starts_with($url, $prefix)) {
        return;
    }
    $key = substr($url, strlen($prefix));

    $credentials = s3_instance_credentials();
    if (!$credentials) {
        return;
    }

    [, $headers] = s3_sign('DELETE', AWS_S3_BUCKET, AWS_S3_REGION, $key, '', $credentials);
    $headerLines = '';
    foreach ($headers as $name => $value) {
        $headerLines .= "$name: $value\r\n";
    }

    $context = stream_context_create(['http' => [
        'method' => 'DELETE',
        'header' => $headerLines,
        'timeout' => 10,
        'ignore_errors' => true,
    ]]);
    @file_get_contents("https://$host/$key", false, $context);
}

// Pulls the HTTP status code out of the $http_response_header array that
// PHP's stream wrapper populates after a file_get_contents() HTTP request.
function s3_response_status($responseHeaders) {
    foreach ($responseHeaders as $line) {
        if (preg_match('#^HTTP/\S+\s+(\d+)#', $line, $m)) {
            return (int)$m[1];
        }
    }
    return 0;
}

// Converts a 0-based row index into a spreadsheet-style row letter:
// 0 -> A, 25 -> Z, 26 -> AA, 27 -> AB, ...
function seat_row_label($index) {
    $label = '';
    $index++;
    while ($index > 0) {
        $index--;
        $label = chr(65 + ($index % 26)) . $label;
        $index = intdiv($index, 26);
    }
    return $label;
}

// A long random opaque token for a ticket's QR code. Deliberately NOT the
// attendee's name/email - anyone who glimpses or photographs a printed QR
// code should not be able to read personal info from it. The check-in
// scanner looks up the attendee/event/seat server-side using this token.
function generate_qr_token() {
    return bin2hex(random_bytes(20));
}
